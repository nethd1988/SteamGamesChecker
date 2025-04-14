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
        private bool isSortedAscending = false;
        private TelegramNotifier telegramNotifier;
        private GameHistoryManager gameHistoryManager;
        private ScanHistoryManager scanHistoryManager;

        public MainForm()
        {
            InitializeComponent();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            scanTimer.Tick += new EventHandler(ScanTimer_Tick);
            scanTimer.Interval = 15 * 60 * 1000; // 15 phút mặc định
            telegramNotifier = TelegramNotifier.Instance;
            gameHistoryManager = GameHistoryManager.Instance;
            scanHistoryManager = ScanHistoryManager.Instance;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            txtAppID.Text = "730";
            cbMethod.Items.AddRange(new[] { "SteamCMD API", "XPAW API", "Steam API" });
            cbMethod.SelectedIndex = 0;
            txtScanInterval.Text = "15"; // Mặc định 15 phút

            lvGameHistory.ColumnClick += new ColumnClickEventHandler(lvGameHistory_ColumnClick);
            lvScanHistory.ColumnClick += new ColumnClickEventHandler(lvScanHistory_ColumnClick);

            LoadGameIDs();
            UpdateTelegramMenuStatus();
            cbMethod.SelectedIndexChanged += new EventHandler(cbMethod_SelectedIndexChanged);
            lbSavedIDs.SelectedIndexChanged += new EventHandler(lbSavedIDs_SelectedIndexChanged);
            lbSavedIDs.DoubleClick += new EventHandler(lbSavedIDs_DoubleClick);

            LoadGameHistoryToListView();
            LoadScanHistoryToListView();
            SortGamesByLastUpdate(false); // Mới nhất lên đầu khi mở tool
            ShowLastScanTime();
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
                "Steam Games Checker v1.2.0\n\n" +
                "Ứng dụng kiểm tra thông tin và cập nhật của game trên Steam.\n\n" +
                "Tính năng:\n" +
                "- Kiểm tra thông tin cập nhật game qua nhiều API\n" +
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
            DateTime nextScan = DateTime.Now.AddMilliseconds(scanTimer.Interval);
            Task.Run(async () =>
            {
                try
                {
                    this.Invoke(new Action(() =>
                    {
                        lblStatus.Text = "Trạng thái: Đang quét tự động...";
                        lblStatus.ForeColor = Color.Blue;
                    }));

                    await ScanAllGames();

                    this.Invoke(new Action(() =>
                    {
                        lblStatus.Text += $" - Lần quét tiếp theo: {nextScan.ToString("HH:mm:ss")}";
                    }));
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

            if (ascending)
            {
                items.Sort((a, b) =>
                {
                    string appIdA = a.Tag.ToString();
                    string appIdB = b.Tag.ToString();

                    if (gameHistory.ContainsKey(appIdA) &&
                        gameHistory.ContainsKey(appIdB) &&
                        gameHistory[appIdA].LastUpdateDateTime.HasValue &&
                        gameHistory[appIdB].LastUpdateDateTime.HasValue)
                    {
                        return gameHistory[appIdA].LastUpdateDateTime.Value.CompareTo(gameHistory[appIdB].LastUpdateDateTime.Value);
                    }
                    return 0;
                });
            }
            else
            {
                items.Sort((a, b) =>
                {
                    string appIdA = a.Tag.ToString();
                    string appIdB = b.Tag.ToString();

                    if (gameHistory.ContainsKey(appIdA) &&
                        gameHistory.ContainsKey(appIdB) &&
                        gameHistory[appIdA].LastUpdateDateTime.HasValue &&
                        gameHistory[appIdB].LastUpdateDateTime.HasValue)
                    {
                        return gameHistory[appIdB].LastUpdateDateTime.Value.CompareTo(gameHistory[appIdA].LastUpdateDateTime.Value);
                    }
                    return 0;
                });
            }

            lvGameHistory.Items.AddRange(items.ToArray());
            lvGameHistory.EndUpdate();
        }

        private void SortScanHistoryByTime(bool ascending)
        {
            lvScanHistory.BeginUpdate();
            List<ListViewItem> items = new List<ListViewItem>();

            foreach (ListViewItem item in lvScanHistory.Items)
            {
                items.Add(item);
            }

            lvScanHistory.Items.Clear();

            if (ascending)
            {
                items.Sort((a, b) =>
                {
                    DateTime timeA = DateTime.ParseExact(a.SubItems[0].Text.Split(' ')[0] + " " + a.SubItems[0].Text.Split(' ')[1], "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                    DateTime timeB = DateTime.ParseExact(b.SubItems[0].Text.Split(' ')[0] + " " + b.SubItems[0].Text.Split(' ')[1], "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                    return timeA.CompareTo(timeB);
                });
            }
            else
            {
                items.Sort((a, b) =>
                {
                    DateTime timeA = DateTime.ParseExact(a.SubItems[0].Text.Split(' ')[0] + " " + a.SubItems[0].Text.Split(' ')[1], "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                    DateTime timeB = DateTime.ParseExact(b.SubItems[0].Text.Split(' ')[0] + " " + b.SubItems[0].Text.Split(' ')[1], "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                    return timeB.CompareTo(timeA);
                });
            }

            lvScanHistory.Items.AddRange(items.ToArray());
            lvScanHistory.EndUpdate();
        }

        private void lvGameHistory_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == 2)
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
                if (info != null && !string.IsNullOrEmpty(info.Name) && info.Name != "Không xác định")
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
                if (info != null && !string.IsNullOrEmpty(info.Name) && info.Name != "Không xác định")
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
                    return info;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi dùng Steam API: {ex.Message}");
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
                                long timestamp = (long)gameData.depots.branches.@public.timeupdated;
                                DateTime updateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                                    .AddSeconds(timestamp);

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

                            if (gameData.time_updated != null)
                            {
                                long timestamp = (long)gameData.time_updated;
                                DateTime updateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                                    .AddSeconds(timestamp);

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

                            if (gameData.release_date != null)
                            {
                                info.ReleaseDate = gameData.release_date.date;
                            }

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

            ScanHistoryItem scanHistory = new ScanHistoryItem();
            scanHistory.TotalGames = total;
            scanHistory.ApiMethod = cbMethod.SelectedItem.ToString();

            foreach (object item in lbSavedIDs.Items)
            {
                if (item == null) continue;

                string selectedItem = item.ToString();
                Match match = Regex.Match(selectedItem, @"\((\d+)\)$");

                if (match.Success)
                {
                    current++;
                    string appID = match.Groups[1].Value;
                    string gameName = selectedItem.Substring(0, selectedItem.LastIndexOf('(')).Trim();

                    lblStatus.Text = $"Trạng thái: Đang quét [{current}/{total}] - {gameName} (ID: {appID})";
                    lblStatus.ForeColor = Color.Blue;
                    UpdateProgressBar(current, total);
                    Application.DoEvents();

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

                        if (gameInfo == null || string.IsNullOrEmpty(gameInfo.Name) || gameInfo.Name == "Không xác định")
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
                            UpdateListViewItem(gameInfo);

                            if (!gameHistory.ContainsKey(appID))
                            {
                                gameHistory.Add(appID, gameInfo);
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
                            }
                            else
                            {
                                gameHistory[appID] = gameInfo;
                                UpdateListViewItem(gameInfo);
                            }

                            if (isNewUpdate && gameInfo.HasRecentUpdate)
                            {
                                updatedGames.Add(gameInfo.Name);
                                try
                                {
                                    await telegramNotifier.SendGameUpdateNotification(gameInfo);
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

                    await Task.Delay(500);
                }
            }

            scanHistory.SuccessCount = successCount;
            scanHistory.FailCount = failedCount;
            scanHistory.UpdatedGames = updatedGames;
            scanHistoryManager.AddScanHistory(scanHistory);
            LoadScanHistoryToListView();

            gameHistoryManager.SaveLastScanTime();

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

            if (failedCount > 0)
            {
                string failedList = string.Join(Environment.NewLine, failedGames);
                MessageBox.Show($"Không thể lấy thông tin của {failedCount} game sau:{Environment.NewLine}{failedList}",
                    "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            toolStripProgressBar1.Visible = false;
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