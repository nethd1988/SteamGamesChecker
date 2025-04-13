using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace SteamGamesChecker
{
    public partial class MainForm : Form
    {
        private const string STEAMDB_URL_BASE = "https://steamdb.info/app/";
        private const string STEAM_API_URL = "https://store.steampowered.com/api/appdetails?appids=";
        private const string XPAW_API_URL = "https://steamapi.xpaw.me/v1/api.php?action=appinfo&appids=";
        private Dictionary<string, GameInfo> gameHistory = new Dictionary<string, GameInfo>();
        private System.Windows.Forms.Timer scanTimer = new System.Windows.Forms.Timer(); // Chỉ định rõ loại Timer
        private string idListPath = "game_ids.txt";
        private bool isSortedAscending = false;
        private TelegramNotifier telegramNotifier; // Biến thành viên telegramNotifier

        public MainForm()
        {
            InitializeComponent();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            // Cài đặt Timer
            scanTimer.Tick += new EventHandler(ScanTimer_Tick);
            scanTimer.Interval = 3600000; // 1 giờ mặc định

            // Khởi tạo Telegram Notifier
            telegramNotifier = TelegramNotifier.Instance;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Default ID for Counter-Strike 2
            txtAppID.Text = "730";

            // Default method
            cbMethod.SelectedIndex = 1; // Selenium

            // Add XPAW method to combobox
            cbMethod.Items.Add("XPAW API");

            // Add sort button to ListView column header
            lvGameHistory.ColumnClick += new ColumnClickEventHandler(lvGameHistory_ColumnClick);

            // Load saved game IDs
            LoadGameIDs();

            // Cập nhật trạng thái Telegram trong menu
            UpdateTelegramMenuStatus();
        }

        /// <summary>
        /// Cập nhật trạng thái menu Telegram dựa trên cấu hình hiện tại
        /// </summary>
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

        // Các phương thức thao tác với Telegram 
        /// <summary>
        /// Hiển thị form cấu hình Telegram
        /// </summary>
        private void telegramMenuItem_Click(object sender, EventArgs e)
        {
            ShowTelegramConfigForm();
        }

        /// <summary>
        /// Hiển thị form cấu hình Telegram từ nút cấu hình
        /// </summary>
        private void btnConfigTelegram_Click(object sender, EventArgs e)
        {
            ShowTelegramConfigForm();
        }

        /// <summary>
        /// Hiển thị form cấu hình Telegram
        /// </summary>
        private void ShowTelegramConfigForm()
        {
            using (TelegramConfigForm configForm = new TelegramConfigForm())
            {
                configForm.ShowDialog();
                // Cập nhật trạng thái menu sau khi đóng form cấu hình
                UpdateTelegramMenuStatus();
            }
        }

        /// <summary>
        /// Xử lý sự kiện menu File > Thoát
        /// </summary>
        private void exitMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Xử lý sự kiện menu Trợ giúp > Giới thiệu
        /// </summary>
        private void aboutMenuItem_Click(object sender, EventArgs e)
        {
            string aboutText =
                "Steam Games Checker v1.1.0\n\n" +
                "Ứng dụng kiểm tra thông tin và cập nhật của game trên Steam.\n\n" +
                "Tính năng:\n" +
                "- Kiểm tra thông tin cập nhật game\n" +
                "- Theo dõi nhiều game cùng lúc\n" +
                "- Tự động quét game định kỳ\n" +
                "- Thông báo cập nhật qua Telegram\n\n" +
                "© 2025";

            MessageBox.Show(aboutText, "Giới thiệu", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // Phương thức xử lý sự kiện ScanTimer_Tick
        private void ScanTimer_Tick(object sender, EventArgs e)
        {
            // Gọi hàm quét tất cả game một cách bất đồng bộ
            Task.Run(async () => {
                await ScanAllGames();
            });
        }

        // Hàm cập nhật ListView
        private void UpdateListViewItem(GameInfo gameInfo)
        {
            if (gameInfo == null || string.IsNullOrEmpty(gameInfo.AppID))
                return;

            foreach (ListViewItem item in lvGameHistory.Items)
            {
                if (item.Tag.ToString() == gameInfo.AppID)
                {
                    item.SubItems[0].Text = gameInfo.Name;
                    item.SubItems[2].Text = ConvertToVietnamTime(gameInfo.LastUpdate);
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

        // Hàm chuyển đổi thời gian sang định dạng Việt Nam
        private string ConvertToVietnamTime(string timeString)
        {
            if (string.IsNullOrEmpty(timeString) || timeString.Contains("Không"))
                return timeString;

            try
            {
                // Kiểm tra nếu đã có chuỗi định dạng Việt Nam
                if (timeString.Contains("tháng"))
                    return timeString;

                // Phân tích chuỗi thời gian
                DateTime time;
                if (DateTime.TryParse(timeString, out time))
                {
                    // Chuyển đổi sang định dạng Việt Nam
                    return time.ToString("dd MMMM yyyy - HH:mm:ss");
                }

                return timeString;
            }
            catch
            {
                return timeString;
            }
        }

        // Hàm cập nhật thanh tiến trình
        private void UpdateProgressBar(int current, int total)
        {
            if (toolStripProgressBar1.InvokeRequired)
            {
                toolStripProgressBar1.Invoke(new Action(() => {
                    toolStripProgressBar1.Value = current;
                }));
            }
            else
            {
                toolStripProgressBar1.Value = current;
            }
        }

        // Hàm sắp xếp game theo thời gian cập nhật
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
                // Cũ nhất lên đầu
                items.Sort((a, b) => {
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
                // Mới nhất lên đầu
                items.Sort((a, b) => {
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

        // Hàm xử lý sự kiện click vào cột để sắp xếp
        private void lvGameHistory_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Nếu click vào cột thời gian cập nhật (cột thứ 3)
            if (e.Column == 2)
            {
                isSortedAscending = !isSortedAscending;
                SortGamesByLastUpdate(isSortedAscending);
            }
        }

        // Hàm tải danh sách game IDs từ file
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

        // Sửa đổi ScanAllGames để thêm phần gửi thông báo Telegram
        private async Task ScanAllGames()
        {
            if (lbSavedIDs.Items.Count == 0)
                return;

            int total = lbSavedIDs.Items.Count;
            int current = 0;
            int successCount = 0;
            int failedCount = 0;
            List<string> failedGames = new List<string>();

            // Hiển thị thanh tiến trình
            toolStripProgressBar1.Maximum = total;
            toolStripProgressBar1.Value = 0;
            toolStripProgressBar1.Visible = true;

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
                        // Thử tất cả các phương thức cho đến khi thành công
                        GameInfo gameInfo = await TryAllMethods(appID);

                        if (gameInfo != null && !string.IsNullOrEmpty(gameInfo.Name) && gameInfo.Name != "Không xác định")
                        {
                            successCount++;

                            // Cập nhật thông tin datetime
                            gameInfo.UpdateLastUpdateDateTime();

                            // Kiểm tra xem đây là bản cập nhật mới hay không
                            bool isNewUpdate = false;
                            GameInfo oldInfo = null;

                            if (gameHistory.ContainsKey(appID))
                            {
                                oldInfo = gameHistory[appID];
                                // Nếu thời gian cập nhật mới hơn thời gian cũ đã lưu
                                if (gameInfo.LastUpdateDateTime.HasValue && oldInfo.LastUpdateDateTime.HasValue &&
                                    gameInfo.LastUpdateDateTime.Value > oldInfo.LastUpdateDateTime.Value)
                                {
                                    isNewUpdate = true;
                                }
                            }
                            else
                            {
                                // Game mới được thêm vào, coi như có cập nhật mới
                                isNewUpdate = gameInfo.HasRecentUpdate;
                            }

                            // Cập nhật vào gameHistory và ListView
                            if (!gameHistory.ContainsKey(appID))
                            {
                                gameHistory.Add(appID, gameInfo);

                                // Thêm vào ListView
                                ListViewItem lvItem = new ListViewItem(gameInfo.Name);
                                lvItem.SubItems.Add(gameInfo.AppID);
                                lvItem.SubItems.Add(ConvertToVietnamTime(gameInfo.LastUpdate));
                                lvItem.SubItems.Add(gameInfo.UpdateDaysCount.ToString());
                                lvItem.Tag = appID;

                                if (gameInfo.HasRecentUpdate)
                                {
                                    lvItem.BackColor = Color.LightGreen;
                                }

                                lvGameHistory.Items.Add(lvItem);
                            }
                            else
                            {
                                gameHistory[appID] = gameInfo;
                                UpdateListViewItem(gameInfo);
                            }

                            // Gửi thông báo Telegram nếu tìm thấy bản cập nhật mới
                            if (isNewUpdate && gameInfo.HasRecentUpdate)
                            {
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

                    // Thêm delay nhỏ để tránh quá tải API
                    await Task.Delay(500);
                }
            }

            // Hiển thị kết quả quét
            string resultMessage = $"Quét hoàn tất: {successCount} thành công, {failedCount} thất bại";
            lblStatus.Text = resultMessage;

            if (successCount > 0)
            {
                lblStatus.ForeColor = failedCount > 0 ? Color.Orange : Color.Green;

                // Sắp xếp lại danh sách theo thời gian cập nhật
                SortGamesByLastUpdate(false); // Mới nhất lên đầu
            }
            else
            {
                lblStatus.ForeColor = Color.Red;
            }

            // Hiển thị danh sách game thất bại nếu có
            if (failedCount > 0)
            {
                string failedList = string.Join(Environment.NewLine, failedGames);
                MessageBox.Show($"Không thể lấy thông tin của {failedCount} game sau:{Environment.NewLine}{failedList}",
                    "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            toolStripProgressBar1.Visible = false;
        }

        // Phương thức thử tất cả các phương pháp để lấy thông tin game
        private async Task<GameInfo> TryAllMethods(string appID)
        {
            GameInfo info = null;

            // Thử phương thức XPAW API
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

            // Thử phương thức Steam API
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

            // Nếu không có phương thức nào thành công, trả về thông tin trống
            return new GameInfo { AppID = appID };
        }

        // Phương thức lấy thông tin game từ XPAW API
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
                        dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

                        if (data != null && data.success == true && data.data != null && data.data[appID] != null)
                        {
                            var gameData = data.data[appID];

                            info.Name = gameData.common?.name ?? "Không xác định";
                            info.Developer = gameData.extended?.developer ?? "Không có thông tin";
                            info.Publisher = gameData.extended?.publisher ?? "Không có thông tin";

                            // Lấy thông tin cập nhật
                            if (gameData.time_updated != null)
                            {
                                long timestamp = (long)gameData.time_updated;
                                DateTime updateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                                    .AddSeconds(timestamp);

                                info.LastUpdate = updateTime.ToString("dd MMMM yyyy - HH:mm:ss UTC");
                                info.LastUpdateDateTime = updateTime;
                                info.UpdateDaysCount = (int)(DateTime.UtcNow - updateTime).TotalDays;
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

        // Phương thức lấy thông tin game từ Steam API
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
                        dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

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

                            // Lưu ý: Steam API không cung cấp thời gian cập nhật
                            // Chúng ta sẽ duy trì thông tin cập nhật hiện tại nếu có

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
    }
}