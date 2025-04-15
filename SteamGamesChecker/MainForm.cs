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
using System.Text;

namespace SteamGamesChecker
{
    public partial class MainForm : Form
    {
        private const string STEAMDB_URL_BASE = "https://steamdb.info/app/";
        private const string STEAM_API_URL = "https://store.steampowered.com/api/appdetails?appids=";
        private const string XPAW_API_URL = "https://steamapi.xpaw.me/v1/api.php?action=appinfo&appids=";
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

        // Lưu cấu hình ứng dụng
        private class AppConfig
        {
            public int ScanInterval { get; set; } = 15; // Mặc định quét mỗi 15 phút
            public bool AutoScanEnabled { get; set; } = false; // Mặc định không quét tự động
            public int SelectedApiMethod { get; set; } = 0; // 0: SteamCMD API, 1: XPAW API, 2: Steam API
            public bool UseSteamDbFallback { get; set; } = true; // Mặc định sử dụng SteamDB khi API khác không trả kết quả
        }

        private AppConfig appConfig = new AppConfig();

        public MainForm()
        {
            InitializeComponent();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            scanTimer.Tick += new EventHandler(ScanTimer_Tick);
            scanTimer.Interval = 15 * 60 * 1000; // 15 phút mặc định
            telegramNotifier = TelegramNotifier.Instance;
            gameHistoryManager = GameHistoryManager.Instance;
            LoadScanHistoryManager();
            LoadConfig();

            // Xử lý đóng form
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

            // Lưu cấu hình trước khi thoát
            SaveConfig();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            txtAppID.Text = "730";
            cbMethod.Items.AddRange(new[] { "SteamCMD API", "XPAW API", "Steam API" });

            // Áp dụng cài đặt từ file config
            ApplyConfig();

            lvGameHistory.ColumnClick += new ColumnClickEventHandler(lvGameHistory_ColumnClick);
            if (scanHistoryManager != null)
            {
                lvScanHistory.ColumnClick += new ColumnClickEventHandler(lvScanHistory_ColumnClick);
            }

            LoadGameIDs();
            UpdateTelegramMenuStatus();
            cbMethod.SelectedIndexChanged += new EventHandler(cbMethod_SelectedIndexChanged);
            lbSavedIDs.SelectedIndexChanged += new EventHandler(lbSavedIDs_SelectedIndexChanged);
            lbSavedIDs.DoubleClick += new EventHandler(lbSavedIDs_DoubleClick);

            LoadGameHistoryToListView();
            LoadScanHistoryToListView();
            SortGamesByLastUpdate(false); // Mới nhất lên đầu khi mở tool
            ShowLastScanTime();

            // Khởi động quét tự động nếu được bật
            if (appConfig.AutoScanEnabled)
            {
                StartAutoScan();
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
                // Sử dụng cấu hình mặc định nếu có lỗi
                appConfig = new AppConfig();
            }
        }

        private void SaveConfig()
        {
            try
            {
                // Cập nhật các cấu hình hiện tại
                appConfig.ScanInterval = int.Parse(txtScanInterval.Text);
                appConfig.AutoScanEnabled = scanTimer.Enabled;
                appConfig.SelectedApiMethod = cbMethod.SelectedIndex;

                // Lưu vào file
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
                // Đảm bảo cập nhật thông tin ngày
                game.UpdateLastUpdateDateTime();

                ListViewItem item = new ListViewItem(game.Name);
                item.SubItems.Add(game.AppID);
                item.SubItems.Add(game.GetVietnameseTimeFormat());
                item.SubItems.Add(game.UpdateDaysCount.ToString());
                item.Tag = game.AppID;

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
                "- Định dạng giờ GMT+7 (Việt Nam)\n\n" +
                "© 2025";

            MessageBox.Show(aboutText, "Giới thiệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void cbMethod_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Lưu cài đặt phương thức API
            appConfig.SelectedApiMethod = cbMethod.SelectedIndex;
            SaveConfig();

            switch (cbMethod.SelectedIndex)
            {
                case 0:
                    lblStatus.Text = "Đã chọn: SteamCMD API - Lấy thông tin chi tiết từ SteamCMD";
                    lblStatus.ForeColor = Color.DarkGreen;
                    break;
                case 1:
                    lblStatus.Text = "Đã chọn: XPAW API - Lấy thông tin từ XPAW Steam API";
                    lblStatus.ForeColor = Color.DarkBlue;
                    break;
                case 2:
                    lblStatus.Text = "Đã chọn: Steam Store API - Lấy thông tin từ Steam Store";
                    lblStatus.ForeColor = Color.DarkGreen;
                    break;
            }
        }

        private void lbSavedIDs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbSavedIDs.SelectedIndex != -1)
            {
                string selectedItem = lbSavedIDs.SelectedItem.ToString();
                Match match = Regex.Match(selectedItem, @"\((\d+)\)$");

                if (match.Success)
                {
                    string appID = match.Groups[1].Value;
                    txtAppID.Text = appID;
                }
            }
        }

        private void lbSavedIDs_DoubleClick(object sender, EventArgs e)
        {
            if (lbSavedIDs.SelectedIndex != -1)
            {
                string selectedItem = lbSavedIDs.SelectedItem.ToString();
                Match match = Regex.Match(selectedItem, @"\((\d+)\)$");

                if (match.Success)
                {
                    string appID = match.Groups[1].Value;
                    txtAppID.Text = appID;
                    btnCheckUpdate_Click(sender, e);
                }
            }
        }

        private void ScanTimer_Tick(object sender, EventArgs e)
        {
            // Đặt lại khoảng thời gian quét về mặc định
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

            foreach (ListViewItem item in lvGameHistory.Items)
            {
                if (item.Tag.ToString() == gameInfo.AppID)
                {
                    item.SubItems[0].Text = gameInfo.Name;
                    item.SubItems[2].Text = gameInfo.GetVietnameseTimeFormat();
                    item.SubItems[3].Text = gameInfo.UpdateDaysCount.ToString();

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
                    toolStripProgressBar1.Value = current;
                });
            }
            else
            {
                toolStripProgressBar1.Value = current;
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

                // Kiểm tra xem cả hai ID có trong gameHistory không
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

                        // Xử lý chuỗi thời gian có định dạng "dd/MM/yyyy HH:mm:ss (GMT+7)"
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
            GameInfo info = null;

            try
            {
                info = await GetGameInfoFromSteamCMD(appID);
                if (info != null && !string.IsNullOrEmpty(info.Name) && info.Name != "Không xác định"
                    && info.LastUpdateDateTime.HasValue)
                {
                    return info;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi dùng SteamCMD API: {ex.Message}");
            }

            try
            {
                info = await GetGameInfoFromXPAW(appID);
                if (info != null && !string.IsNullOrEmpty(info.Name) && info.Name != "Không xác định"
                    && info.LastUpdateDateTime.HasValue)
                {
                    return info;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi dùng XPAW API: {ex.Message}");
            }

            try
            {
                info = await GetGameInfoFromSteamAPI(appID);
                if (info != null && !string.IsNullOrEmpty(info.Name) && info.Name != "Không xác định")
                {
                    // Nếu đã có info nhưng không có thông tin cập nhật
                    if (info.LastUpdateDateTime == null || !info.LastUpdateDateTime.HasValue)
                    {
                        // Thử dùng SteamDB nếu được phép
                        if (appConfig.UseSteamDbFallback)
                        {
                            try
                            {
                                GameInfo steamDbInfo = await GetGameInfoFromSteamDB(appID);
                                if (steamDbInfo != null && !string.IsNullOrEmpty(steamDbInfo.Name) &&
                                    steamDbInfo.Name != "Không xác định" && steamDbInfo.LastUpdateDateTime.HasValue)
                                {
                                    // Kết hợp thông tin từ nhiều nguồn nếu cần
                                    if (string.IsNullOrEmpty(steamDbInfo.Developer) && !string.IsNullOrEmpty(info.Developer))
                                    {
                                        steamDbInfo.Developer = info.Developer;
                                    }

                                    if (string.IsNullOrEmpty(steamDbInfo.Publisher) && !string.IsNullOrEmpty(info.Publisher))
                                    {
                                        steamDbInfo.Publisher = info.Publisher;
                                    }

                                    return steamDbInfo;
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Lỗi khi dùng SteamDB: {ex.Message}");
                            }
                        }
                    }

                    return info;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi dùng Steam API: {ex.Message}");
            }

            // Cuối cùng, thử SteamDB nếu tất cả các phương thức khác đều thất bại
            if (appConfig.UseSteamDbFallback)
            {
                try
                {
                    GameInfo steamDbInfo = await GetGameInfoFromSteamDB(appID);
                    if (steamDbInfo != null && !string.IsNullOrEmpty(steamDbInfo.Name) &&
                        steamDbInfo.Name != "Không xác định" && steamDbInfo.LastUpdateDateTime.HasValue)
                    {
                        return steamDbInfo;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi khi dùng SteamDB: {ex.Message}");
                }
            }

            return new GameInfo { AppID = appID };
        }

        private async Task<GameInfo> GetGameInfoFromSteamCMD(string appID)
        {
            using (HttpClient client = new HttpClient())
            {
                GameInfo info = new GameInfo();
                info.AppID = appID;

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

                                // Đảm bảo timeupdated tồn tại và là số
                                if (gameData.depots.branches.@public.timeupdated != null &&
                                    long.TryParse(gameData.depots.branches.@public.timeupdated.ToString(), out timestamp))
                                {
                                    DateTime updateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                                        .AddSeconds(timestamp);

                                    // Chuyển sang GMT+7
                                    updateTime = updateTime.AddHours(7);

                                    info.LastUpdate = updateTime.ToString("dd/MM/yyyy HH:mm:ss") + " (GMT+7)";
                                    info.LastUpdateDateTime = updateTime;
                                    info.UpdateDaysCount = (int)(DateTime.Now - updateTime).TotalDays;
                                    info.HasRecentUpdate = info.UpdateDaysCount < telegramNotifier.NotificationThreshold;
                                }
                            }

                            // Nếu không có thông tin cập nhật từ SteamCMD, thử dùng thông tin thay đổi cuối cùng
                            if (info.LastUpdateDateTime == null && gameData._change_number != null)
                            {
                                // Ghi lại thông tin change number nhưng không có timestamp
                                info.Status = $"Có thay đổi (Change #{gameData._change_number})";
                            }

                            return info;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SteamCMD API Error: {ex.Message}");
                    throw;
                }

                return info;
            }
        }

        private async Task<GameInfo> GetGameInfoFromXPAW(string appID)
        {
            using (HttpClient client = new HttpClient())
            {
                GameInfo info = new GameInfo();
                info.AppID = appID;

                try
                {
                    string url = $"{XPAW_API_URL}{appID}";
                    HttpResponseMessage response = await client.GetAsync(url);

                    if (response.IsSuccessStatusCode)
                    {
                        string json = await response.Content.ReadAsStringAsync();
                        dynamic data = JsonConvert.DeserializeObject(json);

                        if (data != null && data.success == true && data.data != null && data.data[appID] != null)
                        {
                            var gameData = data.data[appID];

                            info.Name = gameData.common?.name ?? "Không xác định";
                            info.Developer = gameData.extended?.developer ?? "Không có thông tin";
                            info.Publisher = gameData.extended?.publisher ?? "Không có thông tin";

                            // Kiểm tra nếu có dữ liệu time_updated
                            long timestamp = 0;
                            if (gameData.time_updated != null &&
                                long.TryParse(gameData.time_updated.ToString(), out timestamp) &&
                                timestamp > 0)
                            {
                                DateTime updateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                                    .AddSeconds(timestamp);

                                // Chuyển sang giờ Việt Nam (UTC+7)
                                updateTime = updateTime.AddHours(7);

                                info.LastUpdate = updateTime.ToString("dd/MM/yyyy HH:mm:ss") + " (GMT+7)";
                                info.LastUpdateDateTime = updateTime;
                                info.UpdateDaysCount = (int)(DateTime.Now - updateTime).TotalDays;
                                info.HasRecentUpdate = info.UpdateDaysCount < telegramNotifier.NotificationThreshold;
                            }

                            return info;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"XPAW API Error: {ex.Message}");
                    throw;
                }

                return info;
            }
        }

        private async Task<GameInfo> GetGameInfoFromSteamAPI(string appID)
        {
            using (HttpClient client = new HttpClient())
            {
                GameInfo info = new GameInfo();
                info.AppID = appID;

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

                                // Thử phân tích ngày phát hành để có thông tin ngày
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

                            // Steam API thường không cung cấp thông tin cập nhật gần đây
                            return info;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Steam API Error: {ex.Message}");
                    throw;
                }

                return info;
            }
        }

        private async Task<GameInfo> GetGameInfoFromSteamDB(string appID)
        {
            using (HttpClient client = new HttpClient())
            {
                GameInfo info = new GameInfo();
                info.AppID = appID;

                try
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
                    client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8");
                    client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.5");

                    // Tạo cookie để tránh bị phát hiện là bot
                    var cookieContainer = new CookieContainer();
                    var handler = new HttpClientHandler() { CookieContainer = cookieContainer };
                    using (var browserClient = new HttpClient(handler))
                    {
                        string url = $"{STEAMDB_URL_BASE}{appID}/";

                        // Thêm delay ngẫu nhiên để giảm nguy cơ bị chặn
                        await Task.Delay(new Random().Next(1000, 3000));

                        HttpResponseMessage response = await browserClient.GetAsync(url);
                        if (response.IsSuccessStatusCode)
                        {
                            string html = await response.Content.ReadAsStringAsync();

                            // Phân tích dữ liệu HTML để lấy thông tin cập nhật
                            // Lấy tên game
                            var nameMatch = Regex.Match(html, @"<h1\s+itemprop=""name""\s*>(.*?)</h1>", RegexOptions.Singleline);
                            if (nameMatch.Success)
                            {
                                info.Name = HttpUtility.HtmlDecode(nameMatch.Groups[1].Value.Trim());
                            }

                            // Lấy thông tin nhà phát triển
                            var devMatch = Regex.Match(html, @"Developer</td>\s*<td[^>]*>(.*?)</td>", RegexOptions.Singleline);
                            if (devMatch.Success)
                            {
                                info.Developer = HttpUtility.HtmlDecode(devMatch.Groups[1].Value.Trim());
                            }

                            // Lấy thông tin nhà phát hành
                            var pubMatch = Regex.Match(html, @"Publisher</td>\s*<td[^>]*>(.*?)</td>", RegexOptions.Singleline);
                            if (pubMatch.Success)
                            {
                                info.Publisher = HttpUtility.HtmlDecode(pubMatch.Groups[1].Value.Trim());
                            }

                            // Lấy ngày phát hành
                            var releaseDateMatch = Regex.Match(html, @"Release Date</td>\s*<td[^>]*>(.*?)</td>", RegexOptions.Singleline);
                            if (releaseDateMatch.Success)
                            {
                                info.ReleaseDate = HttpUtility.HtmlDecode(releaseDateMatch.Groups[1].Value.Trim());
                            }

                            // Lấy thông tin cập nhật gần đây nhất
                            // Tìm timestamp từ div chứa thông tin thời gian
                            var updateTimestampMatch = Regex.Match(html, @"<td\s+data-sort=""(\d+)""\s*data-time");
                            if (updateTimestampMatch.Success)
                            {
                                long timestamp;
                                if (long.TryParse(updateTimestampMatch.Groups[1].Value, out timestamp))
                                {
                                    DateTime updateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                                        .AddSeconds(timestamp);

                                    // Chuyển sang giờ Việt Nam (UTC+7)
                                    updateTime = updateTime.AddHours(7);

                                    info.LastUpdate = updateTime.ToString("dd/MM/yyyy HH:mm:ss") + " (GMT+7)";
                                    info.LastUpdateDateTime = updateTime;
                                    info.UpdateDaysCount = (int)(DateTime.Now - updateTime).TotalDays;
                                    info.HasRecentUpdate = info.UpdateDaysCount < telegramNotifier.NotificationThreshold;

                                    // Tìm change number 
                                    var changeNumberMatch = Regex.Match(html, @"<td\s+class=""text-monospace"">\s*(\d+)\s*</td>");
                                    if (changeNumberMatch.Success)
                                    {
                                        long changeNumber;
                                        if (long.TryParse(changeNumberMatch.Groups[1].Value, out changeNumber))
                                        {
                                            info.ChangeNumber = changeNumber;
                                        }
                                    }
                                }
                            }

                            return info;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"SteamDB Error: {ex.Message}");
                    throw;
                }

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

            toolStripProgressBar1.Maximum = total;
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

                        if (selectedMethod == 0)
                        {
                            try
                            {
                                gameInfo = await GetGameInfoFromSteamCMD(appID);
                            }
                            catch { }
                        }
                        else if (selectedMethod == 1)
                        {
                            try
                            {
                                gameInfo = await GetGameInfoFromXPAW(appID);
                            }
                            catch { }
                        }
                        else if (selectedMethod == 2)
                        {
                            try
                            {
                                gameInfo = await GetGameInfoFromSteamAPI(appID);
                            }
                            catch { }
                        }

                        if (gameInfo == null || string.IsNullOrEmpty(gameInfo.Name) || gameInfo.Name == "Không xác định"
                            || !gameInfo.LastUpdateDateTime.HasValue)
                        {
                            gameInfo = await TryAllMethods(appID);
                        }

                        if (gameInfo != null && !string.IsNullOrEmpty(gameInfo.Name) && gameInfo.Name != "Không xác định")
                        {
                            successCount++;
                            gameInfo.UpdateLastUpdateDateTime();

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
                        }
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        failedGames.Add($"{gameName} (ID: {appID}) - Lỗi: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"Lỗi khi quét game {gameName} (ID: {appID}): {ex.Message}");
                    }

                    // Thêm độ trễ nhỏ để tránh quá tải server
                    await Task.Delay(500);
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
                        gameInfo = await GetGameInfoFromXPAW(appID);
                        break;
                    case 2:
                        gameInfo = await GetGameInfoFromSteamAPI(appID);
                        break;
                    default:
                        gameInfo = await TryAllMethods(appID);
                        break;
                }

                if (gameInfo == null || string.IsNullOrEmpty(gameInfo.Name) || gameInfo.Name == "Không xác định"
                    || !gameInfo.LastUpdateDateTime.HasValue)
                {
                    // Thử tất cả các phương thức nếu phương thức mặc định không hoạt động
                    gameInfo = await TryAllMethods(appID);
                }

                if (gameInfo != null && !string.IsNullOrEmpty(gameInfo.Name) && gameInfo.Name != "Không xác định")
                {
                    gameInfo.UpdateLastUpdateDateTime();

                    GameInfo oldInfo = gameHistoryManager.GetGameInfo(appID);
                    bool isNewUpdate = gameHistoryManager.AddOrUpdateGameInfo(gameInfo);

                    if (!gameHistory.ContainsKey(appID))
                    {
                        gameHistory.Add(appID, gameInfo);

                        ListViewItem lvItem = new ListViewItem(gameInfo.Name);
                        lvItem.SubItems.Add(gameInfo.AppID);
                        lvItem.SubItems.Add(gameInfo.GetVietnameseTimeFormat());
                        lvItem.SubItems.Add(gameInfo.UpdateDaysCount.ToString());
                        lvItem.Tag = appID;

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
            if (lbSavedIDs.SelectedIndex != -1)
            {
                string selectedItem = lbSavedIDs.SelectedItem.ToString();
                lbSavedIDs.Items.RemoveAt(lbSavedIDs.SelectedIndex);
                SaveGameIDs();
                MessageBox.Show($"Đã xóa {selectedItem} khỏi danh sách theo dõi!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một game để xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

                // Lưu trạng thái cấu hình - đã tắt tự động quét
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

                    // Lưu trạng thái cấu hình - đã bật tự động quét
                    appConfig.AutoScanEnabled = true;
                    appConfig.ScanInterval = minutes;
                    SaveConfig();

                    DateTime nextScan = DateTime.Now.AddMilliseconds(scanTimer.Interval);
                    lblStatus.Text += $" - Lần quét tiếp theo: {nextScan.ToString("HH:mm:ss")}";

                    Task.Run(async () =>
                    {
                        await ScanAllGames();
                    });
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

        // Lớp quản lý lịch sử quét
        private class ScanHistoryManager
        {
            private const string HISTORY_FILE = "scan_history.json";
            private List<ScanHistoryItem> scanHistoryItems = new List<ScanHistoryItem>();
            private const int MAX_HISTORY_ITEMS = 100; // Giới hạn số lượng lịch sử lưu trữ

            public ScanHistoryManager()
            {
                LoadScanHistory();
            }

            // Thêm một lịch sử quét mới
            public void AddScanHistory(ScanHistoryItem historyItem)
            {
                if (historyItem == null)
                    return;

                // Thêm vào đầu danh sách
                scanHistoryItems.Insert(0, historyItem);

                // Giới hạn số lượng lịch sử
                if (scanHistoryItems.Count > MAX_HISTORY_ITEMS)
                {
                    scanHistoryItems.RemoveRange(MAX_HISTORY_ITEMS, scanHistoryItems.Count - MAX_HISTORY_ITEMS);
                }

                // Lưu lịch sử
                SaveScanHistory();
            }

            // Lấy tất cả lịch sử quét
            public List<ScanHistoryItem> GetAllScanHistory()
            {
                return new List<ScanHistoryItem>(scanHistoryItems);
            }

            // Xóa tất cả lịch sử quét
            public void ClearScanHistory()
            {
                scanHistoryItems.Clear();
                SaveScanHistory();
            }

            // Lưu lịch sử quét vào file
            private void SaveScanHistory()
            {
                try
                {
                    string json = JsonConvert.SerializeObject(scanHistoryItems, Formatting.Indented);
                    File.WriteAllText(HISTORY_FILE, json);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi khi lưu lịch sử quét: {ex.Message}");
                }
            }

            // Đọc lịch sử quét từ file
            private void LoadScanHistory()
            {
                try
                {
                    if (File.Exists(HISTORY_FILE))
                    {
                        string json = File.ReadAllText(HISTORY_FILE);
                        var history = JsonConvert.DeserializeObject<List<ScanHistoryItem>>(json);
                        if (history != null)
                        {
                            scanHistoryItems = history;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi khi đọc lịch sử quét: {ex.Message}");
                    scanHistoryItems = new List<ScanHistoryItem>();
                }
            }
        }

        // Lớp chứa thông tin lịch sử một lần quét
        private class ScanHistoryItem
        {
            // Thời gian quét
            public DateTime ScanTime { get; set; }

            // Tổng số game quét
            public int TotalGames { get; set; }

            // Số game quét thành công
            public int SuccessCount { get; set; }

            // Số game quét thất bại
            public int FailCount { get; set; }

            // Danh sách game có cập nhật mới
            public List<string> UpdatedGames { get; set; }

            // Phương thức API sử dụng
            public string ApiMethod { get; set; }

            // Constructor
            public ScanHistoryItem()
            {
                ScanTime = DateTime.Now;
                TotalGames = 0;
                SuccessCount = 0;
                FailCount = 0;
                UpdatedGames = new List<string>();
                ApiMethod = "Unknown";
            }

            // Lấy chuỗi hiển thị danh sách game cập nhật
            public string GetUpdatedGamesString()
            {
                if (UpdatedGames == null || UpdatedGames.Count == 0)
                    return "Không có";

                if (UpdatedGames.Count <= 3)
                    return string.Join(", ", UpdatedGames);

                // Hiển thị 3 game đầu tiên và số lượng game còn lại
                return string.Join(", ", UpdatedGames.Take(3)) + $" và {UpdatedGames.Count - 3} game khác";
            }

            // Lấy chuỗi thời gian quét định dạng Việt Nam
            public string GetScanTimeString()
            {
                return ScanTime.ToString("dd/MM/yyyy HH:mm:ss") + " (GMT+7)";
            }
        }
    }
}