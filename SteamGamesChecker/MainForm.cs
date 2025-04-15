using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using Newtonsoft.Json;
using System.IO;
using System.Web;
using HtmlAgilityPack;

namespace SteamGamesChecker
{
    public partial class MainForm : Form
    {
        private const string STEAMDB_URL_BASE = "https://steamdb.info/app/";
        private const string STEAM_API_URL = "https://store.steampowered.com/api/appdetails?appids=";
        private const string STEAMCMD_API_URL = "https://api.steamcmd.net/v1/info/";
        private Dictionary<string, GameInfo> gameHistory = new Dictionary<string, GameInfo>();
        private System.Windows.Forms.Timer scanTimer = new System.Windows.Forms.Timer();
        private string idListPath = "game_ids.txt";
        private string configPath = "app_config.json";
        private bool isSortedAscending = false;
        private TelegramNotifier telegramNotifier;
        private GameHistoryManager gameHistoryManager;
        private ScanHistoryManager scanHistoryManager;
        private bool isClosing = false;
        private ImageList gameIconImageList;
        private static CookieContainer cookieContainer = new CookieContainer();

        private class AppConfig
        {
            public int ScanInterval { get; set; } = 15;
            public bool AutoScanEnabled { get; set; } = false;
            public int SelectedApiMethod { get; set; } = 0;
            public bool UseSteamDbFallback { get; set; } = true;
        }

        private AppConfig appConfig = new AppConfig();

        public MainForm()
        {
            InitializeComponent();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            scanTimer.Tick += new EventHandler(ScanTimer_Tick);
            scanTimer.Interval = 15 * 60 * 1000;
            telegramNotifier = TelegramNotifier.Instance;
            gameHistoryManager = GameHistoryManager.Instance;
            LoadScanHistoryManager();
            LoadConfig();

            this.FormClosing += MainForm_FormClosing;
        }

        private void LoadScanHistoryManager()
        {
            try
            {
                scanHistoryManager = new ScanHistoryManager();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi khởi tạo ScanHistoryManager: {ex.Message}");
                scanHistoryManager = null;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            isClosing = true;
            SaveConfig();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            txtAppID.Text = "730";
            cbMethod.Items.AddRange(new[] { "SteamCMD API", "Steam API", "SteamDB" });
            ApplyConfig();

            InitializeImageList();

            lvGameHistory.ColumnClick += new ColumnClickEventHandler(lvGameHistory_ColumnClick);
            if (scanHistoryManager != null)
            {
                lvScanHistory.ColumnClick += new ColumnClickEventHandler(lvScanHistory_ColumnClick);
            }

            LoadGameIDs();
            UpdateTelegramMenuStatus();
            cbMethod.SelectedIndexChanged += new EventHandler(cbMethod_SelectedIndexChanged);

            LoadGameHistoryToListView();
            LoadScanHistoryToListView();
            SortGamesByLastUpdate(false);
            ShowLastScanTime();

            // Cài đặt màu sắc cho thanh tiến trình
            toolStripProgressBar1.BackColor = Color.LightGray;
            toolStripProgressBar1.ForeColor = Color.Green;

            if (appConfig.AutoScanEnabled)
            {
                StartAutoScan();
            }
        }

        private void InitializeImageList()
        {
            gameIconImageList = new ImageList();
            gameIconImageList.ImageSize = new Size(24, 24);
            gameIconImageList.ColorDepth = ColorDepth.Depth32Bit;

            string iconsDirectory = Path.Combine(Application.StartupPath, "icons");
            if (!Directory.Exists(iconsDirectory))
            {
                Directory.CreateDirectory(iconsDirectory);
            }

            string defaultIconPath = Path.Combine(iconsDirectory, "default.png");
            try
            {
                if (!File.Exists(defaultIconPath))
                {
                    using (Bitmap defaultIcon = new Bitmap(24, 24))
                    {
                        using (Graphics g = Graphics.FromImage(defaultIcon))
                        {
                            g.Clear(Color.DarkGray);
                            g.DrawRectangle(Pens.White, 0, 0, 23, 23);
                            g.DrawString("?", new Font("Arial", 12), Brushes.White, 6, 2);
                        }
                        defaultIcon.Save(defaultIconPath, System.Drawing.Imaging.ImageFormat.Png);
                    }
                }

                gameIconImageList.Images.Add("default", Image.FromFile(defaultIconPath));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi tạo icon mặc định: {ex.Message}");
            }

            try
            {
                string[] iconFiles = Directory.GetFiles(iconsDirectory, "*.png");
                foreach (string iconFile in iconFiles)
                {
                    string appId = Path.GetFileNameWithoutExtension(iconFile);
                    if (appId != "default" && !gameIconImageList.Images.ContainsKey(appId))
                    {
                        try
                        {
                            gameIconImageList.Images.Add(appId, Image.FromFile(iconFile));
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Lỗi khi tải icon {iconFile}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi tải các icon từ thư mục: {ex.Message}");
            }

            lvGameHistory.SmallImageList = gameIconImageList;
        }

        private void LoadGameIcon(string appId)
        {
            try
            {
                string iconPath = Path.Combine(Application.StartupPath, "icons", $"{appId}.png");
                if (File.Exists(iconPath) && !gameIconImageList.Images.ContainsKey(appId))
                {
                    gameIconImageList.Images.Add(appId, Image.FromFile(iconPath));

                    foreach (ListViewItem item in lvGameHistory.Items)
                    {
                        if (item.Tag.ToString() == appId)
                        {
                            item.ImageKey = appId;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi tải icon cho AppID {appId}: {ex.Message}");
            }
        }

        private async Task<bool> DownloadGameIcon(string appId, HtmlAgilityPack.HtmlDocument document = null)
        {
            try
            {
                string iconsDirectory = Path.Combine(Application.StartupPath, "icons");
                if (!Directory.Exists(iconsDirectory))
                {
                    Directory.CreateDirectory(iconsDirectory);
                }

                string iconFilePath = Path.Combine(iconsDirectory, $"{appId}.png");
                if (File.Exists(iconFilePath))
                {
                    return true;
                }

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/118.0.0.0 Safari/537.36");

                    string iconUrl = null;
                    if (document != null)
                    {
                        var imgNode = document.DocumentNode.SelectSingleNode("//div[contains(@class, 'app-icon')]/img") ??
                                    document.DocumentNode.SelectSingleNode("//div[contains(@class, 'app-header')]//img") ??
                                    document.DocumentNode.SelectSingleNode("//img[@class='app-logo']");

                        if (imgNode != null)
                        {
                            iconUrl = imgNode.GetAttributeValue("src", null);
                            if (!string.IsNullOrEmpty(iconUrl))
                            {
                                if (iconUrl.StartsWith("//"))
                                {
                                    iconUrl = "https:" + iconUrl;
                                }
                                else if (iconUrl.StartsWith("/"))
                                {
                                    iconUrl = "https://steamdb.info" + iconUrl;
                                }
                                System.Diagnostics.Debug.WriteLine($"Tìm thấy URL icon từ SteamDB: {iconUrl}");
                            }
                        }
                    }

                    if (string.IsNullOrEmpty(iconUrl))
                    {
                        string[] cdnFormats = new string[]
                        {
                            $"https://cdn.cloudflare.steamstatic.com/steam/apps/{appId}/header.jpg",
                            $"https://cdn.cloudflare.steamstatic.com/steam/apps/{appId}/capsule_184x69.jpg",
                            $"https://cdn.cloudflare.steamstatic.com/steam/apps/{appId}/library_600x900_2x.jpg",
                            $"https://cdn.akamai.steamstatic.com/steam/apps/{appId}/header.jpg",
                            $"https://cdn.akamai.steamstatic.com/steam/apps/{appId}/capsule_184x69.jpg",
                            $"https://cdn.akamai.steamstatic.com/steam/apps/{appId}/library_600x900_2x.jpg",
                            $"https://steamcdn-a.akamaihd.net/steam/apps/{appId}/header.jpg",
                            $"https://steamcdn-a.akamaihd.net/steam/apps/{appId}/capsule_184x69.jpg",
                            $"https://steamcdn-a.akamaihd.net/steamcommunity/public/images/apps/{appId}/header.jpg",
                            $"https://steamcdn-a.akamaihd.net/steamcommunity/public/images/apps/{appId}/{appId}.jpg"
                        };

                        foreach (string format in cdnFormats)
                        {
                            try
                            {
                                var response = await client.GetAsync(format);
                                if (response.IsSuccessStatusCode)
                                {
                                    iconUrl = format;
                                    System.Diagnostics.Debug.WriteLine($"Tìm thấy URL icon từ Steam CDN: {iconUrl}");
                                    break;
                                }
                            }
                            catch { }
                        }

                        if (string.IsNullOrEmpty(iconUrl))
                        {
                            try
                            {
                                string apiUrl = $"https://store.steampowered.com/api/appdetails?appids={appId}";
                                var response = await client.GetAsync(apiUrl);

                                if (response.IsSuccessStatusCode)
                                {
                                    string json = await response.Content.ReadAsStringAsync();
                                    dynamic data = JsonConvert.DeserializeObject(json);

                                    if (data != null && data[appId]?.success == true && data[appId]?.data != null)
                                    {
                                        var gameData = data[appId].data;
                                        if (gameData.header_image != null)
                                        {
                                            iconUrl = gameData.header_image.ToString();
                                            System.Diagnostics.Debug.WriteLine($"Tìm thấy URL icon từ Steam API: {iconUrl}");
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Lỗi khi lấy URL icon từ Steam API: {ex.Message}");
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(iconUrl))
                    {
                        var imageResponse = await client.GetAsync(iconUrl);
                        if (imageResponse.IsSuccessStatusCode)
                        {
                            byte[] imageData = await imageResponse.Content.ReadAsByteArrayAsync();

                            using (MemoryStream ms = new MemoryStream(imageData))
                            {
                                try
                                {
                                    using (Image originalImage = Image.FromStream(ms))
                                    {
                                        using (Image thumbnail = originalImage.GetThumbnailImage(32, 32, null, IntPtr.Zero))
                                        {
                                            thumbnail.Save(iconFilePath, System.Drawing.Imaging.ImageFormat.Png);
                                            System.Diagnostics.Debug.WriteLine($"Đã lưu icon thành công cho AppID {appId}");
                                            return true;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Lỗi khi xử lý hình ảnh: {ex.Message}");
                                    try
                                    {
                                        File.WriteAllBytes(iconFilePath, imageData);
                                        System.Diagnostics.Debug.WriteLine($"Đã lưu ảnh gốc cho AppID {appId}");
                                        return true;
                                    }
                                    catch (Exception ex2)
                                    {
                                        System.Diagnostics.Debug.WriteLine($"Lỗi khi lưu ảnh gốc: {ex2.Message}");
                                    }
                                }
                            }
                        }
                    }
                }

                try
                {
                    using (Bitmap defaultIcon = new Bitmap(32, 32))
                    {
                        using (Graphics g = Graphics.FromImage(defaultIcon))
                        {
                            g.Clear(Color.DarkGray);
                            g.DrawRectangle(Pens.White, 0, 0, 31, 31);
                            g.DrawString(appId.Substring(0, Math.Min(appId.Length, 4)), new Font("Arial", 8), Brushes.White, 2, 8);
                        }
                        defaultIcon.Save(iconFilePath, System.Drawing.Imaging.ImageFormat.Png);
                        System.Diagnostics.Debug.WriteLine($"Đã tạo icon mặc định cho AppID {appId}");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi khi tạo icon mặc định: {ex.Message}");
                }

                System.Diagnostics.Debug.WriteLine($"Không tìm thấy icon cho AppID {appId}");
                return false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi tải icon cho AppID {appId}: {ex.Message}");
                return false;
            }
        }

        private async Task LoadAllGameIcons()
        {
            try
            {
                List<string> appIds = new List<string>();
                foreach (var item in lbSavedIDs.Items)
                {
                    string listItem = item.ToString();
                    Match match = Regex.Match(listItem, @"\((\d+)\)$");
                    if (match.Success)
                    {
                        string appId = match.Groups[1].Value;
                        appIds.Add(appId);
                    }
                }

                foreach (var game in gameHistory.Values)
                {
                    if (!appIds.Contains(game.AppID))
                    {
                        appIds.Add(game.AppID);
                    }
                }

                if (appIds.Count > 0)
                {
                    lblStatus.Text = "Trạng thái: Đang tải icon game...";
                    lblStatus.ForeColor = Color.Blue;

                    int loadedCount = 0;
                    foreach (string appId in appIds)
                    {
                        string iconPath = Path.Combine(Application.StartupPath, "icons", $"{appId}.png");
                        if (!File.Exists(iconPath))
                        {
                            await DownloadGameIcon(appId);
                            loadedCount++;
                        }
                        LoadGameIcon(appId);
                    }

                    lblStatus.Text = $"Trạng thái: Đã tải {loadedCount} icon game mới";
                    lblStatus.ForeColor = Color.Green;

                    foreach (ListViewItem item in lvGameHistory.Items)
                    {
                        string appId = item.Tag.ToString();
                        if (gameIconImageList.Images.ContainsKey(appId))
                        {
                            item.ImageKey = appId;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi tải icon game: {ex.Message}");
                lblStatus.Text = $"Lỗi khi tải icon game: {ex.Message}";
                lblStatus.ForeColor = Color.Red;
            }
        }

        private async void btnLoadIcons_Click(object sender, EventArgs e)
        {
            btnLoadIcons.Enabled = false;
            try
            {
                await LoadAllGameIcons();
                MessageBox.Show("Đã tải xong icon game!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải icon game: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnLoadIcons.Enabled = true;
            }
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var config = JsonConvert.DeserializeObject<AppConfig>(json);
                    if (config != null)
                    {
                        appConfig = config;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi đọc cấu hình: {ex.Message}");
                appConfig = new AppConfig();
            }
        }

        private void SaveConfig()
        {
            try
            {
                appConfig.ScanInterval = int.Parse(txtScanInterval.Text);
                appConfig.AutoScanEnabled = scanTimer.Enabled;
                appConfig.SelectedApiMethod = cbMethod.SelectedIndex;
                string json = JsonConvert.SerializeObject(appConfig, Formatting.Indented);
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi lưu cấu hình: {ex.Message}");
            }
        }

        private void ApplyConfig()
        {
            try
            {
                cbMethod.SelectedIndex = appConfig.SelectedApiMethod;
                txtScanInterval.Text = appConfig.ScanInterval.ToString();
                scanTimer.Interval = appConfig.ScanInterval * 60 * 1000;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi áp dụng cấu hình: {ex.Message}");
                cbMethod.SelectedIndex = 0;
                txtScanInterval.Text = "15";
                scanTimer.Interval = 15 * 60 * 1000;
            }
        }

        private void StartAutoScan()
        {
            try
            {
                if (int.TryParse(txtScanInterval.Text, out int minutes) && minutes > 0)
                {
                    scanTimer.Interval = minutes * 60 * 1000;
                    scanTimer.Start();
                    btnAutoScan.Text = "Dừng Tự Động";
                    btnAutoScan.BackColor = Color.LightGreen;

                    DateTime nextScan = DateTime.Now.AddMilliseconds(scanTimer.Interval);
                    lblStatus.Text = $"Trạng thái: Tự động quét mỗi {minutes} phút - Lần quét tiếp theo: {nextScan.ToString("HH:mm:ss")}";
                    lblStatus.ForeColor = Color.Green;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi bắt đầu quét tự động: {ex.Message}");
            }
        }

        private void ShowLastScanTime()
        {
            DateTime? lastScanTime = gameHistoryManager.GetLastScanTime();
            if (lastScanTime.HasValue)
            {
                lblStatus.Text = $"Lần quét cuối: {lastScanTime.Value.ToString("dd/MM/yyyy HH:mm:ss")} (GMT+7)";
            }
            else
            {
                lblStatus.Text = "Trạng thái: Chưa thực hiện quét nào";
            }
        }

        private void LoadGameHistoryToListView()
        {
            lvGameHistory.Items.Clear();
            var allGames = gameHistoryManager.GetAllGameInfos();

            foreach (var game in allGames)
            {
                game.UpdateLastUpdateDateTime();
                LoadGameIcon(game.AppID);

                ListViewItem item = new ListViewItem(game.Name);
                item.SubItems.Add(game.AppID);
                item.SubItems.Add(game.LastUpdateDateTime.HasValue ? game.GetVietnameseTimeFormat() : "Không có thông tin");
                item.SubItems.Add(game.UpdateDaysCount >= 0 ? game.UpdateDaysCount.ToString() : "-1");
                item.Tag = game.AppID;

                item.ImageKey = gameIconImageList.Images.ContainsKey(game.AppID) ? game.AppID : "default";

                if (game.HasRecentUpdate)
                {
                    item.BackColor = Color.LightGreen;
                }

                lvGameHistory.Items.Add(item);
            }

            gameHistory.Clear();
            foreach (var game in allGames)
            {
                gameHistory[game.AppID] = game;
            }
        }

        private void LoadScanHistoryToListView()
        {
            if (scanHistoryManager == null || lvScanHistory == null)
                return;

            lvScanHistory.Items.Clear();
            var allHistory = scanHistoryManager.GetAllScanHistory();

            foreach (var history in allHistory)
            {
                ListViewItem item = new ListViewItem(history.GetScanTimeString());
                item.SubItems.Add(history.TotalGames.ToString());
                item.SubItems.Add(history.SuccessCount.ToString());
                item.SubItems.Add(history.FailCount.ToString());
                item.SubItems.Add(history.GetUpdatedGamesString());
                lvScanHistory.Items.Add(item);
            }
        }

        private void UpdateTelegramMenuStatus()
        {
            if (telegramNotifier.IsEnabled)
            {
                telegramMenuItem.Text = "Telegram Bot (Đang bật)";
                telegramMenuItem.ForeColor = Color.Green;
            }
            else
            {
                telegramMenuItem.Text = "Telegram Bot";
                telegramMenuItem.ForeColor = SystemColors.ControlText;
            }
        }

        private void telegramMenuItem_Click(object sender, EventArgs e)
        {
            ShowTelegramConfigForm();
        }

        private void ShowTelegramConfigForm()
        {
            using (TelegramConfigForm configForm = new TelegramConfigForm())
            {
                configForm.ShowDialog();
                UpdateTelegramMenuStatus();
            }
        }

        private void exitMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void aboutMenuItem_Click(object sender, EventArgs e)
        {
            string aboutText =
                "Steam Games Checker v1.3.0\n\n" +
                "Ứng dụng kiểm tra thông tin và cập nhật của game trên Steam.\n\n" +
                "Tính năng:\n" +
                "- Kiểm tra thông tin cập nhật game qua nhiều API\n" +
                "- Tự động dùng SteamDB khi API khác không có thông tin\n" +
                "- Theo dõi nhiều game cùng lúc\n" +
                "- Tự động quét game định kỳ\n" +
                "- Lưu lịch sử quét và thông báo\n" +
                "- Thông báo cập nhật qua Telegram\n" +
                "- Hiển thị icon game\n" +
                "- Định dạng giờ GMT+7 (Việt Nam)\n\n" +
                "© 2025";
            MessageBox.Show(aboutText, "Giới thiệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void cbMethod_SelectedIndexChanged(object sender, EventArgs e)
        {
            appConfig.SelectedApiMethod = cbMethod.SelectedIndex;
            SaveConfig();

            switch (cbMethod.SelectedIndex)
            {
                case 0:
                    lblStatus.Text = "Đã chọn: SteamCMD API - Lấy thông tin chi tiết từ SteamCMD";
                    lblStatus.ForeColor = Color.DarkGreen;
                    break;
                case 1:
                    lblStatus.Text = "Đã chọn: Steam Store API - Lấy thông tin từ Steam Store";
                    lblStatus.ForeColor = Color.DarkGreen;
                    break;
                case 2:
                    lblStatus.Text = "Đã chọn: SteamDB - Lấy thông tin từ https://steamdb.info/app/";
                    lblStatus.ForeColor = Color.DarkMagenta;
                    break;
            }
        }

        private void ScanTimer_Tick(object sender, EventArgs e)
        {
            int intervalMinutes = int.Parse(txtScanInterval.Text);
            scanTimer.Interval = intervalMinutes * 60 * 1000;
            DateTime nextScan = DateTime.Now.AddMilliseconds(scanTimer.Interval);
            Task.Run(async () =>
            {
                try
                {
                    if (!isClosing)
                    {
                        this.Invoke(new Action(() =>
                        {
                            lblStatus.Text = "Trạng thái: Đang quét tự động...";
                            lblStatus.ForeColor = Color.Blue;
                        }));
                    }
                    await ScanAllGames();
                    if (!isClosing)
                    {
                        this.Invoke(new Action(() =>
                        {
                            lblStatus.Text += $" - Lần quét tiếp theo: {nextScan.ToString("HH:mm:ss")}";
                        }));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi trong quá trình quét tự động: {ex.Message}");
                }
            });
        }

        private void UpdateListViewItem(GameInfo gameInfo)
        {
            if (gameInfo == null || string.IsNullOrEmpty(gameInfo.AppID))
                return;

            LoadGameIcon(gameInfo.AppID);

            foreach (ListViewItem item in lvGameHistory.Items)
            {
                if (item.Tag.ToString() == gameInfo.AppID)
                {
                    item.SubItems[0].Text = gameInfo.Name;
                    item.SubItems[2].Text = gameInfo.GetVietnameseTimeFormat();
                    item.SubItems[3].Text = gameInfo.UpdateDaysCount.ToString();

                    item.ImageKey = gameIconImageList.Images.ContainsKey(gameInfo.AppID) ? gameInfo.AppID : "default";

                    if (gameInfo.HasRecentUpdate)
                    {
                        item.BackColor = Color.LightGreen;
                    }
                    else
                    {
                        item.BackColor = SystemColors.Window;
                    }
                    break;
                }
            }
        }

        private void UpdateProgressBar(int current, int total)
        {
            if (this.InvokeRequired)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    int percentage = (int)((double)current / total * 100);
                    toolStripProgressBar1.Value = percentage;
                    toolStripLabelPercentage.Text = $"{percentage}%";
                });
            }
            else
            {
                int percentage = (int)((double)current / total * 100);
                toolStripProgressBar1.Value = percentage;
                toolStripLabelPercentage.Text = $"{percentage}%";
            }
        }

        private void SortGamesByLastUpdate(bool ascending)
        {
            lvGameHistory.BeginUpdate();
            List<ListViewItem> items = new List<ListViewItem>();
            foreach (ListViewItem item in lvGameHistory.Items)
            {
                items.Add(item);
            }
            lvGameHistory.Items.Clear();

            items.Sort((a, b) =>
            {
                string appIdA = a.Tag.ToString();
                string appIdB = b.Tag.ToString();
                bool hasA = gameHistory.ContainsKey(appIdA) && gameHistory[appIdA].LastUpdateDateTime.HasValue;
                bool hasB = gameHistory.ContainsKey(appIdB) && gameHistory[appIdB].LastUpdateDateTime.HasValue;
                if (hasA && hasB)
                {
                    if (ascending)
                    {
                        return gameHistory[appIdA].LastUpdateDateTime.Value.CompareTo(gameHistory[appIdB].LastUpdateDateTime.Value);
                    }
                    else
                    {
                        return gameHistory[appIdB].LastUpdateDateTime.Value.CompareTo(gameHistory[appIdA].LastUpdateDateTime.Value);
                    }
                }
                else if (hasA)
                {
                    return ascending ? -1 : 1;
                }
                else if (hasB)
                {
                    return ascending ? 1 : -1;
                }
                return 0;
            });

            lvGameHistory.Items.AddRange(items.ToArray());
            lvGameHistory.EndUpdate();
        }

        private void SortScanHistoryByTime(bool ascending)
        {
            if (lvScanHistory == null) return;
            lvScanHistory.BeginUpdate();
            List<ListViewItem> items = new List<ListViewItem>();
            foreach (ListViewItem item in lvScanHistory.Items)
            {
                items.Add(item);
            }
            lvScanHistory.Items.Clear();

            try
            {
                items.Sort((a, b) =>
                {
                    try
                    {
                        string timeStrA = a.SubItems[0].Text;
                        string timeStrB = b.SubItems[0].Text;
                        string datePartA = timeStrA.Split('(')[0].Trim();
                        string datePartB = timeStrB.Split('(')[0].Trim();
                        DateTime timeA = DateTime.ParseExact(datePartA, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                        DateTime timeB = DateTime.ParseExact(datePartB, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                        return ascending ? timeA.CompareTo(timeB) : timeB.CompareTo(timeA);
                    }
                    catch
                    {
                        return 0;
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi sắp xếp lịch sử quét: {ex.Message}");
            }

            lvScanHistory.Items.AddRange(items.ToArray());
            lvScanHistory.EndUpdate();
        }

        private void lvGameHistory_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == 2 || e.Column == 3)
            {
                isSortedAscending = !isSortedAscending;
                SortGamesByLastUpdate(isSortedAscending);
            }
        }

        private void lvScanHistory_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == 0)
            {
                isSortedAscending = !isSortedAscending;
                SortScanHistoryByTime(isSortedAscending);
            }
        }

        private void LoadGameIDs()
        {
            if (System.IO.File.Exists(idListPath))
            {
                try
                {
                    string[] lines = System.IO.File.ReadAllLines(idListPath);
                    foreach (string line in lines)
                    {
                        string[] parts = line.Split('|');
                        if (parts.Length >= 2)
                        {
                            string name = parts[0].Trim();
                            string appId = parts[1].Trim();
                            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(appId))
                            {
                                lbSavedIDs.Items.Add($"{name} ({appId})");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi đọc file game IDs: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SaveGameIDs()
        {
            try
            {
                using (System.IO.StreamWriter writer = new System.IO.StreamWriter(idListPath))
                {
                    foreach (object item in lbSavedIDs.Items)
                    {
                        string listItem = item.ToString();
                        Match match = Regex.Match(listItem, @"(.+) \((\d+)\)$");
                        if (match.Success)
                        {
                            string name = match.Groups[1].Value.Trim();
                            string id = match.Groups[2].Value;
                            writer.WriteLine($"{name}|{id}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu file game IDs: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task<GameInfo> TryAllMethods(string appID)
        {
            GameInfo info = new GameInfo { AppID = appID };
            bool hasCompleteInfo = false;

            try
            {
                var steamCmdInfo = await GetGameInfoFromSteamCMD(appID);
                if (steamCmdInfo != null && !string.IsNullOrEmpty(steamCmdInfo.Name) &&
                    steamCmdInfo.Name != "Không xác định" && steamCmdInfo.LastUpdateDateTime.HasValue)
                {
                    System.Diagnostics.Debug.WriteLine($"SteamCMD thành công cho AppID {appID}: Cập nhật cuối = {steamCmdInfo.LastUpdate}");
                    await DownloadGameIcon(appID);
                    return steamCmdInfo;
                }
                else if (steamCmdInfo != null)
                {
                    MergeGameInfo(info, steamCmdInfo);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi dùng SteamCMD API cho AppID {appID}: {ex.Message}");
            }

            try
            {
                var steamDbInfo = await GetGameInfoFromSteamDB(appID);
                if (steamDbInfo != null && !string.IsNullOrEmpty(steamDbInfo.Name) &&
                    steamDbInfo.Name != "Không xác định" && steamDbInfo.LastUpdateDateTime.HasValue)
                {
                    System.Diagnostics.Debug.WriteLine($"SteamDB thành công cho AppID {appID}: Cập nhật cuối = {steamDbInfo.LastUpdate}");
                    MergeGameInfo(info, steamDbInfo);
                    if (steamDbInfo.LastUpdateDateTime.HasValue)
                    {
                        hasCompleteInfo = true;
                    }
                }
                else if (steamDbInfo != null)
                {
                    MergeGameInfo(info, steamDbInfo);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi dùng SteamDB cho AppID {appID}: {ex.Message}");
            }

            if (!hasCompleteInfo || string.IsNullOrEmpty(info.Name) || info.Name == "Không xác định")
            {
                try
                {
                    var steamApiInfo = await GetGameInfoFromSteamAPI(appID);
                    if (steamApiInfo != null && !string.IsNullOrEmpty(steamApiInfo.Name) &&
                        steamApiInfo.Name != "Không xác định")
                    {
                        System.Diagnostics.Debug.WriteLine($"Steam API thành công cho AppID {appID}: Tên = {steamApiInfo.Name}");
                        MergeGameInfo(info, steamApiInfo);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi khi dùng Steam API cho AppID {appID}: {ex.Message}");
                }
            }

            if (!hasCompleteInfo || string.IsNullOrEmpty(info.Name) || info.Name == "Không xác định")
            {
                try
                {
                    var altApiInfo = await GetGameInfoFromAlternativeAPI(appID);
                    if (altApiInfo != null && !string.IsNullOrEmpty(altApiInfo.Name) &&
                        altApiInfo.Name != "Không xác định")
                    {
                        System.Diagnostics.Debug.WriteLine($"Alternative API thành công cho AppID {appID}: Tên = {altApiInfo.Name}");
                        MergeGameInfo(info, altApiInfo);
                        if (altApiInfo.LastUpdateDateTime.HasValue)
                        {
                            hasCompleteInfo = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi khi dùng Alternative API cho AppID {appID}: {ex.Message}");
                }
            }

            if (info.LastUpdateDateTime.HasValue)
            {
                info.UpdateLastUpdateDateTime();
            }

            await DownloadGameIcon(appID);

            return info.Name != "Không xác định" ? info : new GameInfo { AppID = appID };
        }

        private void MergeGameInfo(GameInfo target, GameInfo source)
        {
            if (source == null) return;

            if (!string.IsNullOrEmpty(source.Name) && source.Name != "Không xác định")
                target.Name = source.Name;

            if (!string.IsNullOrEmpty(source.Developer) && source.Developer != "Không có thông tin")
                target.Developer = source.Developer;

            if (!string.IsNullOrEmpty(source.Publisher) && source.Publisher != "Không có thông tin")
                target.Publisher = source.Publisher;

            if (!string.IsNullOrEmpty(source.ReleaseDate) && source.ReleaseDate != "Không có thông tin")
                target.ReleaseDate = source.ReleaseDate;

            if (source.LastUpdateDateTime.HasValue && (!target.LastUpdateDateTime.HasValue ||
                source.LastUpdateDateTime.Value > target.LastUpdateDateTime.Value))
            {
                target.LastUpdateDateTime = source.LastUpdateDateTime;
                target.LastUpdate = source.LastUpdate;
                target.UpdateDaysCount = source.UpdateDaysCount;
                target.HasRecentUpdate = source.HasRecentUpdate;
            }

            if (source.ChangeNumber > target.ChangeNumber)
                target.ChangeNumber = source.ChangeNumber;
        }

        private async Task<GameInfo> GetGameInfoFromSteamDB(string appID)
        {
            System.Diagnostics.Debug.WriteLine($"Đang thử lấy thông tin từ SteamDB cho AppID: {appID}");

            var handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
                AllowAutoRedirect = true,
                UseCookies = true,
            };

            GameInfo info = new GameInfo { AppID = appID };

            handler.CookieContainer = cookieContainer;

            var userAgents = new string[]
            {
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/118.0.0.0 Safari/537.36 Edg/118.0.0.0",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:120.0) Gecko/20100101 Firefox/120.0",
                "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/16.6 Safari/605.1.15",
                "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36"
            };

            Random random = new Random();
            string userAgent = userAgents[random.Next(userAgents.Length)];

            int maxRetries = 3;
            int currentRetry = 0;
            bool success = false;

            while (currentRetry < maxRetries && !success)
            {
                using (HttpClient client = new HttpClient(handler))
                {
                    try
                    {
                        client.Timeout = TimeSpan.FromSeconds(30);

                        client.DefaultRequestHeaders.Clear();
                        client.DefaultRequestHeaders.Add("User-Agent", userAgent);
                        client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.7");
                        client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9,vi;q=0.8");
                        client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
                        client.DefaultRequestHeaders.Add("Referer", "https://steamdb.info/");
                        client.DefaultRequestHeaders.Add("Connection", "keep-alive");
                        client.DefaultRequestHeaders.Add("Cache-Control", "max-age=0");
                        client.DefaultRequestHeaders.Add("Sec-Ch-Ua", "\"Google Chrome\";v=\"119\", \"Chromium\";v=\"119\", \"Not?A_Brand\";v=\"24\"");
                        client.DefaultRequestHeaders.Add("Sec-Ch-Ua-Mobile", "?0");
                        client.DefaultRequestHeaders.Add("Sec-Ch-Ua-Platform", "\"Windows\"");
                        client.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "document");
                        client.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "navigate");
                        client.DefaultRequestHeaders.Add("Sec-Fetch-Site", "same-origin");
                        client.DefaultRequestHeaders.Add("Sec-Fetch-User", "?1");
                        client.DefaultRequestHeaders.Add("Upgrade-Insecure-Requests", "1");
                        client.DefaultRequestHeaders.Add("Priority", "u=0, i");
                        client.DefaultRequestHeaders.Add("DNT", "1");

                        int delayMs = random.Next(8000, 15000);
                        System.Diagnostics.Debug.WriteLine($"Delay {delayMs}ms trước khi gửi request...");
                        await Task.Delay(delayMs);

                        string url = currentRetry == 0 ? $"{STEAMDB_URL_BASE}{appID}/" : $"{STEAMDB_URL_BASE}{appID}/history/";
                        System.Diagnostics.Debug.WriteLine($"Đang truy cập URL SteamDB: {url}");

                        var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

                        if (response.IsSuccessStatusCode)
                        {
                            string html = await response.Content.ReadAsStringAsync();
                            System.Diagnostics.Debug.WriteLine($"Đã nhận HTML từ SteamDB ({html.Length} bytes)");

                            if (html.Contains("DDoS protection by Cloudflare") ||
                                html.Contains("Please complete the security check") ||
                                html.Contains("Please wait while we verify") ||
                                html.Contains("Please enable cookies") ||
                                html.Contains("Access denied") ||
                                html.Contains("rate limited"))
                            {
                                System.Diagnostics.Debug.WriteLine("Bị chặn bởi Cloudflare hoặc hệ thống bảo vệ, đang thử lại...");
                                currentRetry++;
                                continue;
                            }

                            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
                            doc.LoadHtml(html);

                            var titleNode = doc.DocumentNode.SelectSingleNode("//title");
                            if (titleNode != null)
                            {
                                string title = titleNode.InnerText.Trim();
                                System.Diagnostics.Debug.WriteLine($"SteamDB - Tiêu đề trang: {title}");

                                if (title.Contains("Error") || title.Contains("Not Found") || title == "SteamDB")
                                {
                                    System.Diagnostics.Debug.WriteLine("SteamDB trả về trang lỗi hoặc không tìm thấy");
                                    currentRetry++;
                                    continue;
                                }
                            }

                            string gameName = ExtractGameNameFromSteamDB(doc, appID);
                            if (!string.IsNullOrEmpty(gameName))
                            {
                                info.Name = gameName;
                                System.Diagnostics.Debug.WriteLine($"SteamDB - Tên: {info.Name}");
                            }

                            var devNode = doc.DocumentNode.SelectSingleNode("//tr[td[text()='Developer']]/td[2]") ??
                                          doc.DocumentNode.SelectSingleNode("//tr[td[contains(text(), 'Developer')]]/td[2]") ??
                                          doc.DocumentNode.SelectSingleNode("//div[contains(text(), 'Developer')]/following-sibling::div");
                            if (devNode != null)
                            {
                                info.Developer = HttpUtility.HtmlDecode(devNode.InnerText.Trim());
                                System.Diagnostics.Debug.WriteLine($"SteamDB - Nhà phát triển: {info.Developer}");
                            }

                            var pubNode = doc.DocumentNode.SelectSingleNode("//tr[td[text()='Publisher']]/td[2]") ??
                                          doc.DocumentNode.SelectSingleNode("//tr[td[contains(text(), 'Publisher')]]/td[2]") ??
                                          doc.DocumentNode.SelectSingleNode("//div[contains(text(), 'Publisher')]/following-sibling::div");
                            if (pubNode != null)
                            {
                                info.Publisher = HttpUtility.HtmlDecode(pubNode.InnerText.Trim());
                                System.Diagnostics.Debug.WriteLine($"SteamDB - Nhà phát hành: {info.Publisher}");
                            }

                            var releaseDateNode = doc.DocumentNode.SelectSingleNode("//tr[td[text()='Release Date']]/td[2]") ??
                                                 doc.DocumentNode.SelectSingleNode("//tr[td[contains(text(), 'Release Date')]]/td[2]") ??
                                                 doc.DocumentNode.SelectSingleNode("//div[contains(text(), 'Release Date')]/following-sibling::div");
                            if (releaseDateNode != null)
                            {
                                info.ReleaseDate = HttpUtility.HtmlDecode(releaseDateNode.InnerText.Trim());
                                System.Diagnostics.Debug.WriteLine($"SteamDB - Ngày phát hành: {info.ReleaseDate}");
                            }

                            DateTime? updateDateTime = ExtractUpdateDateTimeFromSteamDB(doc);

                            if (updateDateTime.HasValue)
                            {
                                DateTime localTime = updateDateTime.Value.AddHours(7);
                                info.LastUpdateDateTime = localTime;
                                info.LastUpdate = localTime.ToString("dd/MM/yyyy HH:mm:ss") + " (GMT+7)";
                                info.UpdateDaysCount = (int)(DateTime.Now - localTime).TotalDays;
                                info.HasRecentUpdate = info.UpdateDaysCount < telegramNotifier.NotificationThreshold;
                                System.Diagnostics.Debug.WriteLine($"SteamDB - Thời gian cập nhật cuối: {info.LastUpdate}, Số ngày trước: {info.UpdateDaysCount}");
                            }

                            var changeNode = doc.DocumentNode.SelectSingleNode("//tr[td[text()='Last Change Number']]/td[2]") ??
                                           doc.DocumentNode.SelectSingleNode("//tr[td[contains(text(), 'Change Number')]]/td[2]");
                            if (changeNode != null)
                            {
                                string changeText = changeNode.InnerText.Trim();
                                Match changeMatch = Regex.Match(changeText, @"\d+");
                                if (changeMatch.Success && long.TryParse(changeMatch.Value, out long changeNumber))
                                {
                                    info.ChangeNumber = changeNumber;
                                    System.Diagnostics.Debug.WriteLine($"SteamDB - Change Number: {info.ChangeNumber}");
                                }
                            }

                            try
                            {
                                await DownloadGameIcon(appID, doc);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Lỗi khi tải icon game từ SteamDB: {ex.Message}");
                            }

                            success = true;
                            return info;
                        }
                        else if ((int)response.StatusCode == 429 || (int)response.StatusCode == 403)
                        {
                            System.Diagnostics.Debug.WriteLine($"SteamDB đã chặn yêu cầu - Mã trạng thái: {response.StatusCode}");
                            currentRetry++;
                            await Task.Delay(random.Next(15000, 30000));
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"SteamDB - HTTP request thất bại. Mã trạng thái: {response.StatusCode}");
                            currentRetry++;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Lỗi SteamDB cho AppID {appID}: {ex.Message}");
                        currentRetry++;
                    }
                }
            }

            return info;
        }

        private string ExtractGameNameFromSteamDB(HtmlAgilityPack.HtmlDocument doc, string appId)
        {
            var nameNode = doc.DocumentNode.SelectSingleNode("//h1[@itemprop='name']") ??
                           doc.DocumentNode.SelectSingleNode("//h1[contains(@class, 'app-name')]") ??
                           doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'app-header')]/h1") ??
                           doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'pagehead')]//h1") ??
                           doc.DocumentNode.SelectSingleNode($"//a[@href='/app/{appId}/']");

            if (nameNode != null)
            {
                return HttpUtility.HtmlDecode(nameNode.InnerText.Trim());
            }

            var titleNode = doc.DocumentNode.SelectSingleNode("//title");
            if (titleNode != null)
            {
                string title = titleNode.InnerText.Trim();
                if (title.Contains(" · "))
                {
                    return title.Split(new[] { " · " }, StringSplitOptions.None)[0].Trim();
                }
            }

            return null;
        }

        private DateTime? ExtractUpdateDateTimeFromSteamDB(HtmlAgilityPack.HtmlDocument doc)
        {
            DateTime? updateDateTime = null;

            var timeNodes = doc.DocumentNode.SelectNodes("//time[@datetime]");
            if (timeNodes != null)
            {
                foreach (var timeNode in timeNodes)
                {
                    string dateAttr = timeNode.GetAttributeValue("datetime", null);
                    if (!string.IsNullOrEmpty(dateAttr) && DateTimeOffset.TryParse(dateAttr, out DateTimeOffset dto))
                    {
                        DateTime utcTime = dto.UtcDateTime;
                        if (!updateDateTime.HasValue || utcTime > updateDateTime.Value)
                        {
                            updateDateTime = utcTime;
                            System.Diagnostics.Debug.WriteLine($"SteamDB - Tìm thấy datetime: {dateAttr}, đã phân tích thành {utcTime}");
                        }
                    }
                }
            }

            var updateNode = doc.DocumentNode.SelectSingleNode("//tr[td[contains(text(), 'Last Record Update')]]/td[2]/time") ??
                           doc.DocumentNode.SelectSingleNode("//tr[td[contains(text(), 'Last Update')]]/td[2]/time") ??
                           doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'package-header')]/following-sibling::div//time");

            if (updateNode != null)
            {
                string dateAttr = updateNode.GetAttributeValue("datetime", null);
                if (!string.IsNullOrEmpty(dateAttr) && DateTimeOffset.TryParse(dateAttr, out DateTimeOffset dto))
                {
                    DateTime utcTime = dto.UtcDateTime;
                    if (!updateDateTime.HasValue || utcTime > updateDateTime.Value)
                    {
                        updateDateTime = utcTime;
                        System.Diagnostics.Debug.WriteLine($"SteamDB - Last update datetime: {dateAttr}, đã phân tích thành {utcTime}");
                    }
                }
                else
                {
                    string innerText = updateNode.InnerText.Trim();
                    System.Diagnostics.Debug.WriteLine($"SteamDB - Nội dung update: {innerText}");
                    ParseDateFromText(innerText, ref updateDateTime);
                }
            }

            var historyRows = doc.DocumentNode.SelectNodes("//table[contains(@class, 'table-records')]/tbody/tr");
            if (historyRows != null && historyRows.Count > 0)
            {
                foreach (var row in historyRows.Take(5))
                {
                    var timeCell = row.SelectSingleNode(".//td[contains(@class, 'timeago')]/time");
                    if (timeCell != null)
                    {
                        string dateAttr = timeCell.GetAttributeValue("datetime", null);
                        if (!string.IsNullOrEmpty(dateAttr) && DateTimeOffset.TryParse(dateAttr, out DateTimeOffset dto))
                        {
                            DateTime utcTime = dto.UtcDateTime;
                            if (!updateDateTime.HasValue || utcTime > updateDateTime.Value)
                            {
                                updateDateTime = utcTime;
                                System.Diagnostics.Debug.WriteLine($"SteamDB - History datetime: {dateAttr}, đã phân tích thành {utcTime}");
                            }
                        }
                    }
                }
            }

            if (!updateDateTime.HasValue)
            {
                var dateTextNodes = doc.DocumentNode.SelectNodes("//td[contains(text(), 'Updated')] | //td[contains(text(), 'Last Record')] | //div[contains(text(), 'Updated')]");
                if (dateTextNodes != null)
                {
                    foreach (var node in dateTextNodes)
                    {
                        string text = node.InnerText.Trim();
                        ParseDateFromText(text, ref updateDateTime);
                    }
                }
            }

            return updateDateTime;
        }

        private void ParseDateFromText(string text, ref DateTime? updateDateTime)
        {
            foreach (Match match in Regex.Matches(text, @"(\d{1,2})\s+([A-Za-z]+)\s+(\d{4})"))
            {
                try
                {
                    string datePart = match.Groups[0].Value;
                    DateTime parsedDate;
                    if (DateTime.TryParse(datePart, out parsedDate))
                    {
                        if (!updateDateTime.HasValue || parsedDate > updateDateTime.Value)
                        {
                            updateDateTime = parsedDate;
                            System.Diagnostics.Debug.WriteLine($"SteamDB - Phân tích ngày từ văn bản: {datePart} thành {parsedDate}");
                        }
                        continue;
                    }

                    int day = int.Parse(match.Groups[1].Value);
                    string monthName = match.Groups[2].Value;
                    int year = int.Parse(match.Groups[3].Value);

                    string[] englishMonths = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
                    int month = -1;

                    for (int i = 0; i < englishMonths.Length; i++)
                    {
                        if (string.Compare(monthName, englishMonths[i], StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            month = i + 1;
                            break;
                        }
                    }

                    if (month > 0)
                    {
                        DateTime newDate = new DateTime(year, month, day);
                        if (!updateDateTime.HasValue || newDate > updateDateTime.Value)
                        {
                            updateDateTime = newDate;
                            System.Diagnostics.Debug.WriteLine($"SteamDB - Phân tích ngày thủ công: {day} {monthName} {year} thành {newDate}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi phân tích ngày: {ex.Message}");
                }
            }

            foreach (Match match in Regex.Matches(text, @"\b(\d{4}[-/]\d{1,2}[-/]\d{1,2})\b"))
            {
                try
                {
                    string datePart = match.Groups[1].Value;
                    DateTime parsedDate;
                    if (DateTime.TryParse(datePart, out parsedDate))
                    {
                        if (!updateDateTime.HasValue || parsedDate > updateDateTime.Value)
                        {
                            updateDateTime = parsedDate;
                            System.Diagnostics.Debug.WriteLine($"SteamDB - Phân tích ngày ISO: {datePart} thành {parsedDate}");
                        }
                    }
                }
                catch { }
            }
        }

        private async Task<GameInfo> GetGameInfoFromSteamCMD(string appID)
        {
            using (HttpClient client = new HttpClient())
            {
                GameInfo info = new GameInfo { AppID = appID };

                try
                {
                    client.DefaultRequestHeaders.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/114.0.0.0 Safari/537.36");
                    string url = $"{STEAMCMD_API_URL}{appID}";
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        dynamic data = JsonConvert.DeserializeObject(json);

                        if (data != null && data.status == "success" && data.data != null && data.data[appID] != null)
                        {
                            var gameData = data.data[appID];

                            if (gameData.common != null && gameData.common.name != null)
                            {
                                info.Name = gameData.common.name;
                            }
                            else
                            {
                                info.Name = "Không xác định";
                            }

                            if (gameData._change_number != null)
                            {
                                info.ChangeNumber = (long)gameData._change_number;
                            }

                            if (gameData.extended != null)
                            {
                                info.Developer = gameData.extended.developer ?? "Không có thông tin";
                                info.Publisher = gameData.extended.publisher ?? "Không có thông tin";
                            }

                            if (gameData.depots != null && gameData.depots.branches != null && gameData.depots.branches.@public != null)
                            {
                                long timestamp = 0;
                                if (gameData.depots.branches.@public.timeupdated != null &&
                                    long.TryParse(gameData.depots.branches.@public.timeupdated.ToString(), out timestamp))
                                {
                                    DateTime updateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                                        .AddSeconds(timestamp);
                                    updateTime = updateTime.AddHours(7);
                                    info.LastUpdate = updateTime.ToString("dd/MM/yyyy HH:mm:ss") + " (GMT+7)";
                                    info.LastUpdateDateTime = updateTime;
                                    info.UpdateDaysCount = (int)(DateTime.Now - updateTime).TotalDays;
                                    info.HasRecentUpdate = info.UpdateDaysCount < telegramNotifier.NotificationThreshold;
                                }
                            }

                            if (info.LastUpdateDateTime == null && gameData._change_number != null)
                            {
                                info.Status = $"Có thay đổi (Change #{gameData._change_number})";
                            }

                            return info;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SteamCMD API Error for AppID {appID}: {ex.Message}");
                    throw;
                }

                return info;
            }
        }

        private async Task<GameInfo> GetGameInfoFromSteamAPI(string appID)
        {
            using (HttpClient client = new HttpClient())
            {
                GameInfo info = new GameInfo { AppID = appID };

                try
                {
                    string url = $"{STEAM_API_URL}{appID}";
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        dynamic data = JsonConvert.DeserializeObject(json);

                        if (data != null && data[appID]?.success == true && data[appID]?.data != null)
                        {
                            var gameData = data[appID].data;
                            info.Name = gameData.name ?? "Không xác định";
                            if (gameData.developers != null && gameData.developers.Count > 0)
                            {
                                info.Developer = gameData.developers[0];
                            }
                            if (gameData.publishers != null && gameData.publishers.Count > 0)
                            {
                                info.Publisher = gameData.publishers[0];
                            }
                            if (gameData.release_date != null && gameData.release_date.date != null)
                            {
                                info.ReleaseDate = gameData.release_date.date;
                                try
                                {
                                    string releaseDateStr = gameData.release_date.date.ToString();
                                    DateTime releaseDate;
                                    if (DateTime.TryParse(releaseDateStr, out releaseDate))
                                    {
                                        info.Status = $"Phát hành: {releaseDate.ToString("dd/MM/yyyy")}";
                                    }
                                }
                                catch { }
                            }
                            return info;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Steam API Error for AppID {appID}: {ex.Message}");
                    throw;
                }

                return info;
            }
        }

        private async Task<GameInfo> GetGameInfoFromAlternativeAPI(string appID)
        {
            GameInfo info = new GameInfo { AppID = appID };

            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36");

                    string steamSpyUrl = $"https://steamspy.com/api.php?request=appdetails&appid={appID}";
                    var response = await client.GetAsync(steamSpyUrl);

                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        dynamic data = JsonConvert.DeserializeObject(json);

                        if (data != null)
                        {
                            if (data.name != null)
                            {
                                info.Name = data.name.ToString();
                                System.Diagnostics.Debug.WriteLine($"SteamSpy - Tên: {info.Name}");
                            }

                            if (data.developer != null)
                            {
                                info.Developer = data.developer.ToString();
                                System.Diagnostics.Debug.WriteLine($"SteamSpy - Nhà phát triển: {info.Developer}");
                            }

                            if (data.publisher != null)
                            {
                                info.Publisher = data.publisher.ToString();
                                System.Diagnostics.Debug.WriteLine($"SteamSpy - Nhà phát hành: {info.Publisher}");
                            }

                            if (data.releasedate != null)
                            {
                                info.ReleaseDate = data.releasedate.ToString();
                                System.Diagnostics.Debug.WriteLine($"SteamSpy - Ngày phát hành: {info.ReleaseDate}");
                            }
                        }
                    }

                    try
                    {
                        string itadApiKey = "YOUR_API_KEY";
                        string itadUrl = $"https://api.isthereanydeal.com/v01/game/info/?key={itadApiKey}&plains={appID}";
                        var itadResponse = await client.GetAsync(itadUrl);

                        if (itadResponse.IsSuccessStatusCode)
                        {
                            string itadJson = await itadResponse.Content.ReadAsStringAsync();
                            dynamic itadData = JsonConvert.DeserializeObject(itadJson);

                            if (itadData != null && itadData.data != null)
                            {
                                if (itadData.data.title != null)
                                {
                                    info.Name = itadData.data.title.ToString();
                                    System.Diagnostics.Debug.WriteLine($"ITAD - Tên: {info.Name}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Lỗi khi truy cập ITAD API: {ex.Message}");
                    }

                    try
                    {
                        string xpawUrl = $"https://steamapi.xpaw.me/api/GetAppChanges/{appID}";
                        var xpawResponse = await client.GetAsync(xpawUrl);

                        if (xpawResponse.IsSuccessStatusCode)
                        {
                            string xpawJson = await xpawResponse.Content.ReadAsStringAsync();
                            dynamic xpawData = JsonConvert.DeserializeObject(xpawJson);

                            if (xpawData != null && xpawData.success == true)
                            {
                                if (xpawData.data?.changes != null)
                                {
                                    foreach (var change in xpawData.data.changes)
                                    {
                                        if (change.timestamp != null)
                                        {
                                            long timestamp = (long)change.timestamp;
                                            DateTime utcTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp);
                                            DateTime localTime = utcTime.AddHours(7);

                                            if (!info.LastUpdateDateTime.HasValue || localTime > info.LastUpdateDateTime.Value)
                                            {
                                                info.LastUpdateDateTime = localTime;
                                                info.LastUpdate = localTime.ToString("dd/MM/yyyy HH:mm:ss") + " (GMT+7)";
                                                info.UpdateDaysCount = (int)(DateTime.Now - localTime).TotalDays;
                                                info.HasRecentUpdate = info.UpdateDaysCount >= 0 && info.UpdateDaysCount < telegramNotifier.NotificationThreshold;
                                                System.Diagnostics.Debug.WriteLine($"SteamAPI.xpaw.me - Thời gian cập nhật: {info.LastUpdate}, Số ngày trước: {info.UpdateDaysCount}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Lỗi khi truy cập SteamAPI.xpaw.me: {ex.Message}");
                    }
                }

                return info;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi dùng Alternative API cho AppID {appID}: {ex.Message}");
                return info;
            }
        }

        private async Task ScanAllGames()
        {
            if (lbSavedIDs.Items.Count == 0)
                return;

            int total = lbSavedIDs.Items.Count;
            int current = 0;
            int successCount = 0;
            int failedCount = 0;
            List<string> failedGames = new List<string>();
            List<string> updatedGames = new List<string>();

            toolStripProgressBar1.Maximum = 100;
            toolStripProgressBar1.Value = 0;
            toolStripProgressBar1.Visible = true;

            ScanHistoryItem scanHistory = null;
            if (scanHistoryManager != null)
            {
                scanHistory = new ScanHistoryItem();
                scanHistory.TotalGames = total;
                scanHistory.ApiMethod = cbMethod.SelectedItem.ToString();
            }

            foreach (object item in lbSavedIDs.Items)
            {
                if (item == null || isClosing) continue;

                string selectedItem = item.ToString();
                Match match = Regex.Match(selectedItem, @"\((\d+)\)$");

                if (match.Success)
                {
                    current++;
                    string appID = match.Groups[1].Value;
                    string gameName = selectedItem.Substring(0, selectedItem.LastIndexOf('(')).Trim();

                    if (!isClosing)
                    {
                        this.Invoke(new Action(() =>
                        {
                            lblStatus.Text = $"Trạng thái: Đang quét [{current}/{total}] - {gameName} (ID: {appID})";
                            lblStatus.ForeColor = Color.Blue;
                            UpdateProgressBar(current, total);
                        }));
                    }

                    try
                    {
                        GameInfo gameInfo = null;
                        int selectedMethod = cbMethod.SelectedIndex;

                        switch (selectedMethod)
                        {
                            case 0:
                                gameInfo = await GetGameInfoFromSteamCMD(appID);
                                break;
                            case 1:
                                gameInfo = await GetGameInfoFromSteamAPI(appID);
                                break;
                            case 2:
                                gameInfo = await GetGameInfoFromSteamDB(appID);
                                break;
                            default:
                                gameInfo = null;
                                break;
                        }

                        if (gameInfo == null || string.IsNullOrEmpty(gameInfo.Name) ||
                            gameInfo.Name == "Không xác định" || !gameInfo.LastUpdateDateTime.HasValue)
                        {
                            gameInfo = await TryAllMethods(appID);
                        }

                        if (gameInfo != null && !string.IsNullOrEmpty(gameInfo.Name) && gameInfo.Name != "Không xác định")
                        {
                            successCount++;
                            gameInfo.UpdateLastUpdateDateTime();

                            await DownloadGameIcon(appID);

                            GameInfo oldInfo = gameHistoryManager.GetGameInfo(appID);
                            bool isNewUpdate = false;

                            if (oldInfo != null)
                            {
                                isNewUpdate = gameInfo.HasNewerUpdate(oldInfo);
                            }
                            else
                            {
                                isNewUpdate = gameInfo.HasRecentUpdate;
                            }

                            gameHistoryManager.AddOrUpdateGameInfo(gameInfo);

                            if (!isClosing)
                            {
                                this.Invoke(new Action(() =>
                                {
                                    LoadGameIcon(appID);
                                    UpdateListViewItem(gameInfo);
                                }));
                            }

                            if (!gameHistory.ContainsKey(appID))
                            {
                                gameHistory.Add(appID, gameInfo);

                                if (!isClosing)
                                {
                                    this.Invoke(new Action(() =>
                                    {
                                        bool existsInListView = false;
                                        foreach (ListViewItem lvItem in lvGameHistory.Items)
                                        {
                                            if (lvItem.Tag.ToString() == appID)
                                            {
                                                existsInListView = true;
                                                break;
                                            }
                                        }

                                        if (!existsInListView)
                                        {
                                            ListViewItem lvItem = new ListViewItem(gameInfo.Name);
                                            lvItem.SubItems.Add(gameInfo.AppID);
                                            lvItem.SubItems.Add(gameInfo.GetVietnameseTimeFormat());
                                            lvItem.SubItems.Add(gameInfo.UpdateDaysCount.ToString());
                                            lvItem.Tag = appID;

                                            lvItem.ImageKey = gameIconImageList.Images.ContainsKey(appID) ? appID : "default";

                                            if (gameInfo.HasRecentUpdate)
                                            {
                                                lvItem.BackColor = Color.LightGreen;
                                            }

                                            lvGameHistory.Items.Add(lvItem);
                                        }
                                    }));
                                }
                            }
                            else
                            {
                                gameHistory[appID] = gameInfo;

                                if (!isClosing)
                                {
                                    this.Invoke(new Action(() =>
                                    {
                                        UpdateListViewItem(gameInfo);
                                    }));
                                }
                            }

                            if (isNewUpdate && gameInfo.HasRecentUpdate)
                            {
                                updatedGames.Add(gameInfo.Name);
                                try
                                {
                                    if (telegramNotifier.IsEnabled)
                                    {
                                        await telegramNotifier.SendGameUpdateNotification(gameInfo);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Lỗi gửi thông báo Telegram: {ex.Message}");
                                }
                            }
                        }
                        else
                        {
                            failedCount++;
                            failedGames.Add($"{gameName} (ID: {appID})");

                            try
                            {
                                await DownloadGameIcon(appID);
                            }
                            catch { }
                        }
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        failedGames.Add($"{gameName} (ID: {appID}) - Lỗi: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"Lỗi khi quét game {gameName} (ID: {appID}): {ex.Message}");

                        try
                        {
                            await DownloadGameIcon(appID);
                        }
                        catch { }
                    }

                    Random random = new Random();
                    await Task.Delay(random.Next(300, 1500));
                }
            }

            if (scanHistoryManager != null && scanHistory != null)
            {
                scanHistory.SuccessCount = successCount;
                scanHistory.FailCount = failedCount;
                scanHistory.UpdatedGames = updatedGames;
                scanHistoryManager.AddScanHistory(scanHistory);

                if (!isClosing)
                {
                    this.Invoke(new Action(() =>
                    {
                        LoadScanHistoryToListView();
                    }));
                }
            }

            gameHistoryManager.SaveLastScanTime();

            if (!isClosing)
            {
                this.Invoke(new Action(() =>
                {
                    string resultMessage = $"Quét hoàn tất: {successCount} thành công, {failedCount} thất bại";
                    if (updatedGames.Count > 0)
                    {
                        resultMessage += $", {updatedGames.Count} cập nhật mới";
                    }
                    lblStatus.Text = resultMessage;
                    if (successCount > 0)
                    {
                        lblStatus.ForeColor = failedCount > 0 ? Color.Orange : Color.Green;
                        SortGamesByLastUpdate(false);
                    }
                    else
                    {
                        lblStatus.ForeColor = Color.Red;
                    }
                    toolStripProgressBar1.Visible = false;
                }));
            }

            if (failedCount > 0 && !isClosing)
            {
                this.Invoke(new Action(() =>
                {
                    string failedList = string.Join(Environment.NewLine, failedGames);
                    MessageBox.Show($"Không thể lấy thông tin của {failedCount} game sau:{Environment.NewLine}{failedList}",
                        "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }));
            }
        }

        private async void btnCheckUpdate_Click(object sender, EventArgs e)
        {
            string appID = txtAppID.Text.Trim();
            if (string.IsNullOrEmpty(appID))
            {
                MessageBox.Show("Vui lòng nhập App ID!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            lblStatus.Text = "Trạng thái: Đang kiểm tra...";
            lblStatus.ForeColor = Color.Blue;
            Application.DoEvents();

            try
            {
                GameInfo gameInfo;

                switch (cbMethod.SelectedIndex)
                {
                    case 0:
                        gameInfo = await GetGameInfoFromSteamCMD(appID);
                        break;
                    case 1:
                        gameInfo = await GetGameInfoFromSteamAPI(appID);
                        break;
                    case 2:
                        gameInfo = await GetGameInfoFromSteamDB(appID);
                        break;
                    default:
                        gameInfo = await TryAllMethods(appID);
                        break;
                }

                if (gameInfo == null || string.IsNullOrEmpty(gameInfo.Name) || gameInfo.Name == "Không xác định" || !gameInfo.LastUpdateDateTime.HasValue)
                {
                    gameInfo = await TryAllMethods(appID);
                }

                if (gameInfo != null && !string.IsNullOrEmpty(gameInfo.Name) && gameInfo.Name != "Không xác định")
                {
                    gameInfo.UpdateLastUpdateDateTime();
                    GameInfo oldInfo = gameHistoryManager.GetGameInfo(appID);
                    bool isNewUpdate = gameHistoryManager.AddOrUpdateGameInfo(gameInfo);

                    await DownloadGameIcon(appID);
                    LoadGameIcon(appID);

                    if (!gameHistory.ContainsKey(appID))
                    {
                        gameHistory.Add(appID, gameInfo);
                        ListViewItem lvItem = new ListViewItem(gameInfo.Name);
                        lvItem.SubItems.Add(gameInfo.AppID);
                        lvItem.SubItems.Add(gameInfo.GetVietnameseTimeFormat());
                        lvItem.SubItems.Add(gameInfo.UpdateDaysCount.ToString());
                        lvItem.Tag = appID;

                        lvItem.ImageKey = gameIconImageList.Images.ContainsKey(appID) ? appID : "default";

                        if (gameInfo.HasRecentUpdate)
                        {
                            lvItem.BackColor = Color.LightGreen;
                        }

                        lvGameHistory.Items.Add(lvItem);
                        lblStatus.Text = $"Trạng thái: Đã kiểm tra {gameInfo.Name} - Cập nhật: {gameInfo.GetVietnameseTimeFormat()}";
                        lblStatus.ForeColor = Color.Green;
                    }
                    else
                    {
                        gameHistory[appID] = gameInfo;
                        UpdateListViewItem(gameInfo);
                        lblStatus.Text = $"Trạng thái: Đã cập nhật {gameInfo.Name} - Cập nhật: {gameInfo.GetVietnameseTimeFormat()}";
                        lblStatus.ForeColor = Color.Green;

                        if (isNewUpdate && gameInfo.HasRecentUpdate)
                        {
                            lblStatus.Text += " - Phát hiện cập nhật mới!";
                            DialogResult result = MessageBox.Show(
                                $"Phát hiện cập nhật mới cho game {gameInfo.Name}!\nBạn có muốn gửi thông báo qua Telegram không?",
                                "Cập nhật mới",
                                MessageBoxButtons.YesNo,
                                MessageBoxIcon.Question);

                            if (result == DialogResult.Yes && telegramNotifier.IsEnabled)
                            {
                                await telegramNotifier.SendGameUpdateNotification(gameInfo);
                                MessageBox.Show("Đã gửi thông báo!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            }
                        }
                    }
                }
                else
                {
                    lblStatus.Text = "Trạng thái: Không tìm thấy thông tin game";
                    lblStatus.ForeColor = Color.Red;
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Trạng thái: Lỗi - {ex.Message}";
                lblStatus.ForeColor = Color.Red;
                System.Diagnostics.Debug.WriteLine($"Lỗi khi kiểm tra cập nhật: {ex.Message}");
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string appID = txtAppID.Text.Trim();
            if (string.IsNullOrEmpty(appID))
            {
                MessageBox.Show("Vui lòng nhập App ID!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string gameName = "Game " + appID;
            foreach (ListViewItem item in lvGameHistory.Items)
            {
                if (item.SubItems[1].Text == appID)
                {
                    gameName = item.SubItems[0].Text;
                    break;
                }
            }

            bool exists = false;
            foreach (object item in lbSavedIDs.Items)
            {
                if (item.ToString().Contains($"({appID})"))
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                lbSavedIDs.Items.Add($"{gameName} ({appID})");
                SaveGameIDs();
                MessageBox.Show($"Đã thêm {gameName} (ID: {appID}) vào danh sách theo dõi!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"Game này đã có trong danh sách theo dõi!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private async void btnScanAll_Click(object sender, EventArgs e)
        {
            await ScanAllGames();
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (lvGameHistory.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = lvGameHistory.SelectedItems[0];
                string appID = selectedItem.SubItems[1].Text; // Cột thứ 2 là ID
                string gameName = selectedItem.SubItems[0].Text; // Cột đầu tiên là tên game

                // Xác nhận xóa
                DialogResult result = MessageBox.Show(
                    $"Bạn có chắc chắn muốn xóa game '{gameName}' với ID {appID} không?",
                    "Xác nhận xóa",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    // Xóa khỏi lvGameHistory
                    lvGameHistory.Items.Remove(selectedItem);

                    // Xóa khỏi gameHistory và gameHistoryManager
                    gameHistory.Remove(appID);
                    gameHistoryManager.RemoveGameInfo(appID);

                    // Xóa khỏi lbSavedIDs
                    for (int i = 0; i < lbSavedIDs.Items.Count; i++)
                    {
                        string item = lbSavedIDs.Items[i].ToString();
                        if (item.Contains($"({appID})"))
                        {
                            lbSavedIDs.Items.RemoveAt(i);
                            break;
                        }
                    }

                    // Lưu thay đổi vào file
                    SaveGameIDs();

                    MessageBox.Show($"Đã xóa game '{gameName}' với ID {appID}!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một game trong danh sách để xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnAutoScan_Click(object sender, EventArgs e)
        {
            if (scanTimer.Enabled)
            {
                scanTimer.Stop();
                btnAutoScan.Text = "Tự Động Quét";
                btnAutoScan.BackColor = SystemColors.Control;
                lblStatus.Text = "Trạng thái: Tự động quét đã dừng";
                lblStatus.ForeColor = SystemColors.ControlText;
                appConfig.AutoScanEnabled = false;
                SaveConfig();
                ShowLastScanTime();
            }
            else
            {
                if (int.TryParse(txtScanInterval.Text, out int minutes) && minutes > 0)
                {
                    scanTimer.Interval = minutes * 60 * 1000;
                    scanTimer.Start();
                    btnAutoScan.Text = "Dừng Tự Động";
                    btnAutoScan.BackColor = Color.LightGreen;
                    lblStatus.Text = $"Trạng thái: Tự động quét mỗi {minutes} phút";
                    lblStatus.ForeColor = Color.Green;
                    appConfig.AutoScanEnabled = true;
                    appConfig.ScanInterval = minutes;
                    SaveConfig();

                    DateTime nextScan = DateTime.Now.AddMilliseconds(scanTimer.Interval);
                    lblStatus.Text += $" - Lần quét tiếp theo: {nextScan.ToString("HH:mm:ss")}";
                    Task.Run(async () => await ScanAllGames());
                }
                else
                {
                    MessageBox.Show("Vui lòng nhập thời gian quét hợp lệ (phút)!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnConfigTelegram_Click(object sender, EventArgs e)
        {
            ShowTelegramConfigForm();
        }

        private void btnClearHistory_Click(object sender, EventArgs e)
        {
            if (scanHistoryManager == null)
            {
                MessageBox.Show("Không thể truy cập lịch sử quét!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DialogResult result = MessageBox.Show(
                "Bạn có chắc chắn muốn xóa tất cả lịch sử quét?",
                "Xác nhận xóa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                scanHistoryManager.ClearScanHistory();
                LoadScanHistoryToListView();
                MessageBox.Show("Đã xóa lịch sử quét!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}