using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Newtonsoft.Json;
using System.Globalization;

namespace SteamGamesChecker
{
    public partial class MainForm : Form
    {
        private const string STEAMCMD_API_URL = "https://api.steamcmd.net/v1/info/";
        private Dictionary<string, GameInfo> gameHistory = new Dictionary<string, GameInfo>();
        private System.Windows.Forms.Timer scanTimer = new System.Windows.Forms.Timer();
        private string idListPath = "game_ids.txt";
        private string configPath = "app_config.json";
        private TelegramNotifier telegramNotifier;
        private GameHistoryManager gameHistoryManager;
        private ScanHistoryManager scanHistoryManager;
        private bool isClosing = false;
        private ImageList gameIconImageList;
        private Dictionary<int, bool> sortStates = new Dictionary<int, bool>(); // true: tăng dần, false: giảm dần
        private RemoteUpdateService _remoteUpdateService;
        private string _clientApiUrl = "http://localhost:7288";
        private string _apiKey = "your-secure-api-key-here";
        private bool _enableRemoteUpdate = false;

        private class AppConfig
        {
            public int ScanInterval { get; set; } = 15;
            public bool AutoScanEnabled { get; set; } = false;
        }

        private class RemoteUpdateConfig
        {
            public string ClientApiUrl { get; set; } = "http://localhost:7288";
            public string ApiKey { get; set; } = "your-secure-api-key-here";
            public bool EnableRemoteUpdate { get; set; } = false;
        }

        private AppConfig appConfig = new AppConfig();

        public MainForm()
        {
            InitializeComponent();
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
            cbMethod.Items.Add("SteamCMD API");
            cbMethod.SelectedIndex = 0;
            cbMethod.Enabled = false;
            ApplyConfig();

            InitializeImageList();

            lvGameHistory.ColumnClick += new ColumnClickEventHandler(lvGameHistory_ColumnClick);
            if (scanHistoryManager != null)
            {
                lvScanHistory.ColumnClick += new ColumnClickEventHandler(lvScanHistory_ColumnClick);
            }

            LoadGameIDs();
            UpdateTelegramMenuStatus();

            LoadGameHistoryToListView();
            LoadScanHistoryToListView();
            SortGamesByLastUpdate(false);
            ShowLastScanTime();

            toolStripProgressBar1.BackColor = Color.LightGray;
            toolStripProgressBar1.ForeColor = Color.Green;

            LoadRemoteUpdateSettings();
            _remoteUpdateService = new RemoteUpdateService(_clientApiUrl, _apiKey);

            ToolStripMenuItem remoteMenuItem = new ToolStripMenuItem("Cấu hình Điều khiển Từ xa");
            remoteMenuItem.Click += (s, ev) => ShowRemoteSettingsDialog();
            toolsToolStripMenuItem.DropDownItems.Add(remoteMenuItem);

            if (appConfig.AutoScanEnabled)
            {
                StartAutoScan();
            }
        }

        private void LoadRemoteUpdateSettings()
        {
            try
            {
                if (File.Exists("remote_config.json"))
                {
                    string json = File.ReadAllText("remote_config.json");
                    var config = JsonConvert.DeserializeObject<RemoteUpdateConfig>(json);
                    if (config != null)
                    {
                        _clientApiUrl = config.ClientApiUrl;
                        _apiKey = config.ApiKey;
                        _enableRemoteUpdate = config.EnableRemoteUpdate;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi đọc cấu hình remote: {ex.Message}");
            }
        }

        private void SaveRemoteUpdateSettings()
        {
            try
            {
                var config = new RemoteUpdateConfig
                {
                    ClientApiUrl = _clientApiUrl,
                    ApiKey = _apiKey,
                    EnableRemoteUpdate = _enableRemoteUpdate
                };

                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText("remote_config.json", json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi lưu cấu hình remote: {ex.Message}");
            }
        }

        private async Task HandleGameUpdateDetected(GameInfo gameInfo)
        {
            if (_enableRemoteUpdate && gameInfo != null && !string.IsNullOrEmpty(gameInfo.AppID))
            {
                try
                {
                    bool success = await _remoteUpdateService.SendUpdateCommandAsync(
                        gameInfo.AppID,
                        $"Cập nhật tự động phát hiện lúc {DateTime.Now:dd/MM/yyyy HH:mm:ss}");

                    if (success)
                    {
                        this.Invoke(new Action(() =>
                        {
                            lblStatus.Text += " - Đã gửi lệnh cập nhật tới client";
                        }));
                    }
                    else
                    {
                        this.Invoke(new Action(() =>
                        {
                            lblStatus.Text += " - Không thể gửi lệnh cập nhật tới client";
                        }));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi khi gửi lệnh cập nhật: {ex.Message}");
                }
            }
        }

        private void ShowRemoteSettingsDialog()
        {
            using (var form = new Form())
            {
                form.Text = "Cấu hình Điều khiển Từ xa";
                form.Size = new Size(400, 250);
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                var lblUrl = new Label { Text = "URL API Client:", Left = 20, Top = 20, Width = 120 };
                var txtUrl = new TextBox { Text = _clientApiUrl, Left = 150, Top = 20, Width = 200 };

                var lblApiKey = new Label { Text = "API Key:", Left = 20, Top = 50, Width = 120 };
                var txtApiKey = new TextBox { Text = _apiKey, Left = 150, Top = 50, Width = 200 };

                var chkEnable = new CheckBox
                {
                    Text = "Bật tính năng gửi lệnh cập nhật tự động",
                    Checked = _enableRemoteUpdate,
                    Left = 20,
                    Top = 80,
                    Width = 330
                };

                var btnTest = new Button { Text = "Kiểm tra kết nối", Left = 20, Top = 110, Width = 120 };
                btnTest.Click += async (s, e) =>
                {
                    try
                    {
                        var service = new RemoteUpdateService(txtUrl.Text, txtApiKey.Text);
                        bool success = await service.SendUpdateCommandAsync("test", "Kiểm tra kết nối");

                        if (success)
                        {
                            MessageBox.Show("Kết nối thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                        else
                        {
                            MessageBox.Show("Kết nối thất bại!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                };

                var btnSave = new Button { Text = "Lưu", Left = 175, Top = 170, Width = 90 };
                btnSave.Click += (s, e) =>
                {
                    _clientApiUrl = txtUrl.Text;
                    _apiKey = txtApiKey.Text;
                    _enableRemoteUpdate = chkEnable.Checked;
                    SaveRemoteUpdateSettings();

                    _remoteUpdateService = new RemoteUpdateService(_clientApiUrl, _apiKey);
                    form.DialogResult = DialogResult.OK;
                };

                var btnCancel = new Button { Text = "Hủy", Left = 275, Top = 170, Width = 90 };
                btnCancel.Click += (s, e) => form.DialogResult = DialogResult.Cancel;

                form.Controls.AddRange(new Control[] { lblUrl, txtUrl, lblApiKey, txtApiKey, chkEnable, btnTest, btnSave, btnCancel });

                form.ShowDialog();
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

        private async Task<bool> DownloadGameIcon(string appId)
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

                    string iconUrl = null;
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

        private async void loadIconsMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                await LoadAllGameIcons();
                MessageBox.Show("Đã tải xong icon game!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải icon game: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void clearHistoryMenuItem_Click(object sender, EventArgs e)
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
                txtScanInterval.Text = appConfig.ScanInterval.ToString();
                scanTimer.Interval = appConfig.ScanInterval * 60 * 1000;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi áp dụng cấu hình: {ex.Message}");
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
                "- Kiểm tra thông tin cập nhật game qua SteamCMD API\n" +
                "- Theo dõi nhiều game cùng lúc\n" +
                "- Tự động quét game định kỳ\n" +
                "- Lưu lịch sử quét và thông báo\n" +
                "- Thông báo cập nhật qua Telegram\n" +
                "- Hiển thị icon game\n" +
                "- Định dạng giờ GMT+7 (Việt Nam)\n" +
                "- Gửi lệnh cập nhật từ xa qua API\n\n" +
                "© 2025";
            MessageBox.Show(aboutText, "Giới thiệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

                    if (gameInfo.HasRecentUpdate && gameInfo.UpdateDaysCount >= 0)
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
                    return ascending ? gameHistory[appIdA].LastUpdateDateTime.Value.CompareTo(gameHistory[appIdB].LastUpdateDateTime.Value) :
                                       gameHistory[appIdB].LastUpdateDateTime.Value.CompareTo(gameHistory[appIdA].LastUpdateDateTime.Value);
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
            int columnIndex = e.Column;

            if (!sortStates.ContainsKey(columnIndex))
            {
                sortStates[columnIndex] = true; // Mặc định là tăng dần
            }
            else
            {
                sortStates[columnIndex] = !sortStates[columnIndex]; // Đảo chiều sắp xếp
            }
            bool ascending = sortStates[columnIndex];

            lvGameHistory.ListViewItemSorter = new ListViewItemComparer(columnIndex, ascending);
            lvGameHistory.Sort();
        }

        private void lvScanHistory_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == 0)
            {
                bool isSortedAscending = sortStates.ContainsKey(e.Column) ? !sortStates[e.Column] : true;
                sortStates[e.Column] = isSortedAscending;
                SortScanHistoryByTime(isSortedAscending);
            }
        }

        private void LoadGameIDs()
        {
            if (File.Exists(idListPath))
            {
                try
                {
                    string[] lines = File.ReadAllLines(idListPath);
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
                using (StreamWriter writer = new StreamWriter(idListPath))
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

                                    TimeSpan timeDiff = DateTime.Now - updateTime;
                                    info.HasRecentUpdate = timeDiff.TotalDays >= 0 && timeDiff.TotalDays <= telegramNotifier.NotificationThreshold;
                                }
                            }

                            if (info.LastUpdateDateTime == null && gameData._change_number != null)
                            {
                                info.Status = $"Có thay đổi (Change #{gameData._change_number})";
                            }

                            return info;
                        }
                    }
                    info.Status = "Không tìm thấy thông tin";
                    return info;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SteamCMD API Error for AppID {appID}: {ex.Message}");
                    info.Status = $"Lỗi: {ex.Message}";
                    return info;
                }
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
                scanHistory.ApiMethod = "SteamCMD API";
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
                        GameInfo gameInfo = await GetGameInfoFromSteamCMD(appID);

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
                                    await HandleGameUpdateDetected(gameInfo);
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Lỗi gửi thông báo: {ex.Message}");
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
                GameInfo gameInfo = await GetGameInfoFromSteamCMD(appID);

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
                            await HandleGameUpdateDetected(gameInfo);
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
                string appID = selectedItem.SubItems[1].Text;
                string gameName = selectedItem.SubItems[0].Text;

                DialogResult result = MessageBox.Show(
                    $"Bạn có chắc chắn muốn xóa game '{gameName}' với ID {appID} không?",
                    "Xác nhận xóa",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    lvGameHistory.Items.Remove(selectedItem);
                    gameHistory.Remove(appID);
                    gameHistoryManager.RemoveGameInfo(appID);

                    for (int i = 0; i < lbSavedIDs.Items.Count; i++)
                    {
                        string item = lbSavedIDs.Items[i].ToString();
                        if (item.Contains($"({appID})"))
                        {
                            lbSavedIDs.Items.RemoveAt(i);
                            break;
                        }
                    }

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
    }

    public class RemoteUpdateService
    {
        private readonly string _apiUrl;
        private readonly string _apiKey;

        public RemoteUpdateService(string apiUrl, string apiKey)
        {
            _apiUrl = apiUrl;
            _apiKey = apiKey;
        }

        public async Task<bool> SendUpdateCommandAsync(string appId, string message)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-API-KEY", _apiKey);
                var content = new StringContent(JsonConvert.SerializeObject(new { AppId = appId, Message = message }), System.Text.Encoding.UTF8, "application/json");
                var response = await client.PostAsync($"{_apiUrl}/api/update", content);
                return response.IsSuccessStatusCode;
            }
        }
    }

    public class ListViewItemComparer : System.Collections.IComparer
    {
        private int column;
        private bool ascending;

        public ListViewItemComparer(int column, bool ascending)
        {
            this.column = column;
            this.ascending = ascending;
        }

        public int Compare(object x, object y)
        {
            ListViewItem itemX = x as ListViewItem;
            ListViewItem itemY = y as ListViewItem;

            if (itemX == null || itemY == null)
                return 0;

            string textX = itemX.SubItems[column].Text;
            string textY = itemY.SubItems[column].Text;

            if (column == 3) // Cột "Ngày"
            {
                int daysX, daysY;
                bool isNumericX = int.TryParse(textX, out daysX);
                bool isNumericY = int.TryParse(textY, out daysY);

                if (isNumericX && isNumericY)
                {
                    return ascending ? daysX.CompareTo(daysY) : daysY.CompareTo(daysX);
                }
                else if (isNumericX)
                {
                    return ascending ? -1 : 1;
                }
                else if (isNumericY)
                {
                    return ascending ? 1 : -1;
                }
                else
                {
                    return ascending ? string.Compare(textX, textY) : string.Compare(textY, textX);
                }
            }
            else if (column == 2) // Cột "Lần Cập Nhật Cuối"
            {
                DateTime dateX = DateTime.MinValue;
                DateTime dateY = DateTime.MinValue;
                bool isDateX = DateTime.TryParseExact(textX, "dd/MM/yyyy HH:mm:ss (GMT+7)", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateX);
                bool isDateY = DateTime.TryParseExact(textY, "dd/MM/yyyy HH:mm:ss (GMT+7)", CultureInfo.InvariantCulture, DateTimeStyles.None, out dateY);

                if (isDateX && isDateY)
                {
                    return ascending ? dateX.CompareTo(dateY) : dateY.CompareTo(dateX);
                }
                else if (isDateX)
                {
                    return ascending ? -1 : 1;
                }
                else if (isDateY)
                {
                    return ascending ? 1 : -1;
                }
                else
                {
                    return ascending ? string.Compare(textX, textY) : string.Compare(textY, textX);
                }
            }
            else // Các cột khác (Tên Game và ID)
            {
                return ascending ? string.Compare(textX, textY) : string.Compare(textY, textX);
            }
        }
    }
}