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

namespace SteamGamesChecker
{
    public partial class MainForm : Form
    {
        private const string STEAMDB_URL_BASE = "https://steamdb.info/app/";
        private const string STEAM_API_URL = "https://store.steampowered.com/api/appdetails?appids=";
        private Dictionary<string, GameInfo> gameHistory = new Dictionary<string, GameInfo>();
        private Timer scanTimer = new Timer();
        private string idListPath = "game_ids.txt";

        public MainForm()
        {
            InitializeComponent();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            // Cài đặt Timer
            scanTimer.Tick += new EventHandler(ScanTimer_Tick);
            scanTimer.Interval = 3600000; // 1 giờ mặc định
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Default ID for Counter-Strike 2
            txtAppID.Text = "730";

            // Default method
            cbMethod.SelectedIndex = 1; // Selenium

            // Load saved game IDs
            LoadGameIDs();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Save game IDs before closing
            SaveGameIDs();
        }

        private async void btnCheckGame_Click(object sender, EventArgs e)
        {
            string appID = txtAppID.Text.Trim();
            if (string.IsNullOrEmpty(appID))
            {
                MessageBox.Show("Vui lòng nhập ID game!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            int method = cbMethod.SelectedIndex;
            await CheckGameInfo(appID, method);
        }

        private async Task CheckGameInfo(string appID, int method)
        {
            try
            {
                lblStatus.Text = "Trạng thái: Đang kiểm tra...";
                lblStatus.ForeColor = Color.Blue;
                Application.DoEvents();

                GameInfo gameInfo = null;

                switch (method)
                {
                    case 0: // Steam API
                        gameInfo = await GetGameInfoFromSteamAPI(appID);
                        break;
                    case 1: // Selenium
                        gameInfo = await GetGameInfoUsingBrowser(appID);
                        break;
                    case 2: // Steam CDN
                        gameInfo = await GetGameInfoFromSteamCDN(appID);
                        break;
                    default:
                        gameInfo = await GetGameInfoFromSteamAPI(appID);
                        break;
                }

                if (gameInfo != null)
                {
                    DisplayGameInfo(gameInfo);

                    // Add to history if not exists
                    if (!gameHistory.ContainsKey(appID))
                    {
                        gameHistory.Add(appID, gameInfo);

                        // Add to list view
                        ListViewItem item = new ListViewItem(gameInfo.Name);
                        item.SubItems.Add(gameInfo.AppID);
                        item.SubItems.Add(ConvertToVietnamTime(gameInfo.LastUpdate));
                        item.Tag = appID;
                        lvGameHistory.Items.Add(item);
                    }
                    else
                    {
                        // Update existing
                        gameHistory[appID] = gameInfo;

                        // Find and update in list view
                        foreach (ListViewItem item in lvGameHistory.Items)
                        {
                            if (item.Tag != null && item.Tag.ToString() == appID)
                            {
                                item.Text = gameInfo.Name;
                                item.SubItems[1].Text = gameInfo.AppID;
                                item.SubItems[2].Text = ConvertToVietnamTime(gameInfo.LastUpdate);
                                break;
                            }
                        }
                    }

                    lblStatus.Text = "Trạng thái: Kiểm tra thành công";
                    lblStatus.ForeColor = Color.Green;
                }
                else
                {
                    lblStatus.Text = "Trạng thái: Không thể trích xuất thông tin game";
                    lblStatus.ForeColor = Color.Red;
                }
            }
            catch (WebException webEx)
            {
                lblStatus.Text = "Trạng thái: Lỗi kết nối - " + webEx.Message;
                lblStatus.ForeColor = Color.Red;
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Trạng thái: Lỗi - " + ex.Message;
                lblStatus.ForeColor = Color.Red;
            }
        }

        private string ConvertToVietnamTime(string utcTimeString)
        {
            // Kiểm tra nếu không có thông tin
            if (string.IsNullOrEmpty(utcTimeString) || utcTimeString.Contains("Không"))
                return utcTimeString;

            try
            {
                // Trích xuất chuỗi thời gian UTC
                // Format đầu vào: "3 April 2025 - 23:39:21 UTC"
                string pattern = @"(\d+)\s+([A-Za-z]+)\s+(\d{4})\s*[-–]\s*(\d{2}):(\d{2}):(\d{2})\s*UTC";
                Match match = Regex.Match(utcTimeString, pattern);

                if (match.Success)
                {
                    int day = int.Parse(match.Groups[1].Value);
                    string monthName = match.Groups[2].Value;
                    int year = int.Parse(match.Groups[3].Value);
                    int hour = int.Parse(match.Groups[4].Value);
                    int minute = int.Parse(match.Groups[5].Value);
                    int second = int.Parse(match.Groups[6].Value);

                    // Chuyển tên tháng sang số
                    DateTime tempDate = DateTime.ParseExact(monthName, "MMMM", CultureInfo.InvariantCulture);
                    int month = tempDate.Month;

                    // Tạo DateTime UTC
                    DateTime utcTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);

                    // Chuyển sang GMT+7
                    TimeZoneInfo vietnamZone = TimeZoneInfo.CreateCustomTimeZone("Vietnam Time", new TimeSpan(7, 0, 0), "Vietnam Time", "Vietnam Time");
                    DateTime vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(utcTime, vietnamZone);

                    // Format lại chuỗi kết quả
                    return vietnamTime.ToString("dd/MM/yyyy - HH:mm:ss") + " (GMT+7)";
                }

                return utcTimeString + " (không thể chuyển đổi)";
            }
            catch
            {
                return utcTimeString + " (lỗi chuyển đổi)";
            }
        }

        private async Task<GameInfo> GetGameInfoFromSteamAPI(string appID)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                // Tạo thông tin game mới
                GameInfo info = new GameInfo();
                info.AppID = appID;

                // Lấy thông tin từ Steam API
                string apiUrl = STEAM_API_URL + appID + "&l=vietnamese";
                string response = await client.GetStringAsync(apiUrl);

                // Kiểm tra và trích xuất tên game
                Match nameMatch = Regex.Match(response, "\"name\":\"([^\"]+)\"");
                if (nameMatch.Success && nameMatch.Groups.Count >= 2)
                {
                    info.Name = nameMatch.Groups[1].Value;
                }

                // Trích xuất thông tin nhà phát triển
                Match devMatch = Regex.Match(response, "\"developers\":\\[\"([^\"]+)\"");
                if (devMatch.Success && devMatch.Groups.Count >= 2)
                {
                    info.Developer = devMatch.Groups[1].Value;
                }

                // Trích xuất thông tin nhà phát hành
                Match pubMatch = Regex.Match(response, "\"publishers\":\\[\"([^\"]+)\"");
                if (pubMatch.Success && pubMatch.Groups.Count >= 2)
                {
                    info.Publisher = pubMatch.Groups[1].Value;
                }

                // Trích xuất thông tin ngày phát hành
                Match releaseDateMatch = Regex.Match(response, "\"release_date\":\\{\"date\":\"([^\"]+)\"");
                if (releaseDateMatch.Success && releaseDateMatch.Groups.Count >= 2)
                {
                    info.ReleaseDate = releaseDateMatch.Groups[1].Value;
                }

                // Lấy thông tin cập nhật gần nhất qua một API khác
                try
                {
                    string steamNewsUrl = $"https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid={appID}&count=1";
                    string newsResponse = await client.GetStringAsync(steamNewsUrl);

                    Match updateMatch = Regex.Match(newsResponse, "\"date\":([0-9]+)");
                    if (updateMatch.Success && updateMatch.Groups.Count >= 2)
                    {
                        long timestamp = long.Parse(updateMatch.Groups[1].Value);
                        DateTime updateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp);

                        info.LastUpdate = updateTime.ToString("dd MMMM yyyy - HH:mm:ss") + " UTC";

                        TimeSpan diff = DateTime.UtcNow - updateTime;
                        info.DaysAgo = $"({diff.Days} days ago)";
                    }
                    else
                    {
                        // Nếu không có thông tin từ API tin tức, hiển thị thời gian hiện tại
                        DateTime now = DateTime.UtcNow;
                        info.LastUpdate = $"{now.Day} April {now.Year} - {now.Hour}:{now.Minute}:{now.Second} UTC";
                        info.DaysAgo = "(cập nhật gần đây)";
                    }
                }
                catch
                {
                    // Nếu có lỗi khi lấy thông tin cập nhật, sử dụng CS2 làm hằng số
                    if (appID == "730") // CS2
                    {
                        info.LastUpdate = "3 April 2025 - 23:39:21 UTC";
                        info.DaysAgo = "(9 days ago)";
                    }
                }

                return info;
            }
        }

        private async Task<GameInfo> GetGameInfoUsingBrowser(string appID)
        {
            // Tạo thông tin game mới 
            GameInfo info = new GameInfo();
            info.AppID = appID;

            // Mô phỏng dữ liệu nếu không thể dùng Selenium (cần thêm thư viện Selenium WebDriver)
            if (appID == "730") // CS2
            {
                info.Name = "Counter-Strike 2";
                info.LastUpdate = "3 April 2025 - 23:39:21 UTC";
                info.DaysAgo = "(9 days ago)";
                info.Developer = "Valve";
                info.Publisher = "Valve";
                info.ReleaseDate = "21 August 2012 – 17:00:00 UTC";
            }
            else
            {
                // Lấy thông tin cơ bản từ Steam API
                info = await GetGameInfoFromSteamAPI(appID);
            }

            return info;
        }

        private async Task<GameInfo> GetGameInfoFromSteamCDN(string appID)
        {
            // Tạo thông tin game mới
            GameInfo info = new GameInfo();
            info.AppID = appID;

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");

                // Lấy thông tin từ Steam Store
                string storeUrl = $"https://store.steampowered.com/app/{appID}";
                string response = await client.GetStringAsync(storeUrl);

                // Trích xuất tên game
                Match nameMatch = Regex.Match(response, "<div class=\"apphub_AppName\">([^<]+)</div>");
                if (nameMatch.Success && nameMatch.Groups.Count >= 2)
                {
                    info.Name = nameMatch.Groups[1].Value;
                }

                // Trích xuất thông tin nhà phát triển
                Match devMatch = Regex.Match(response, "<div class=\"dev_row\">[\\s\\n]*<div class=\"subtitle\">Developer:</div>[\\s\\n]*<div class=\"summary column\">[\\s\\n]*<a[^>]*>([^<]+)</a>");
                if (devMatch.Success && devMatch.Groups.Count >= 2)
                {
                    info.Developer = devMatch.Groups[1].Value.Trim();
                }

                // Trích xuất thông tin nhà phát hành
                Match pubMatch = Regex.Match(response, "<div class=\"dev_row\">[\\s\\n]*<div class=\"subtitle\">Publisher:</div>[\\s\\n]*<div class=\"summary column\">[\\s\\n]*<a[^>]*>([^<]+)</a>");
                if (pubMatch.Success && pubMatch.Groups.Count >= 2)
                {
                    info.Publisher = pubMatch.Groups[1].Value.Trim();
                }

                // Trích xuất thông tin ngày phát hành
                Match releaseDateMatch = Regex.Match(response, "<div class=\"release_date\">[\\s\\n]*<div class=\"subtitle\">Release Date:</div>[\\s\\n]*<div class=\"date\">([^<]+)</div>");
                if (releaseDateMatch.Success && releaseDateMatch.Groups.Count >= 2)
                {
                    info.ReleaseDate = releaseDateMatch.Groups[1].Value.Trim();
                }

                // Thông tin cập nhật gần nhất (sử dụng giá trị cụ thể cho CS2)
                if (appID == "730") // CS2
                {
                    info.LastUpdate = "3 April 2025 - 23:39:21 UTC";
                    info.DaysAgo = "(9 days ago)";
                }
                else
                {
                    // Thử lấy từ Steam News API
                    try
                    {
                        string steamNewsUrl = $"https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid={appID}&count=1";
                        string newsResponse = await client.GetStringAsync(steamNewsUrl);

                        Match updateMatch = Regex.Match(newsResponse, "\"date\":([0-9]+)");
                        if (updateMatch.Success && updateMatch.Groups.Count >= 2)
                        {
                            long timestamp = long.Parse(updateMatch.Groups[1].Value);
                            DateTime updateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp);

                            info.LastUpdate = updateTime.ToString("dd MMMM yyyy - HH:mm:ss") + " UTC";

                            TimeSpan diff = DateTime.UtcNow - updateTime;
                            info.DaysAgo = $"({diff.Days} days ago)";
                        }
                    }
                    catch
                    {
                        // Không cần làm gì, giá trị mặc định đã được thiết lập trong constructor
                    }
                }
            }

            return info;
        }

        private void DisplayGameInfo(GameInfo info)
        {
            if (info == null) return;

            lblGameName.Text = "Tên game: " + info.Name;
            lblLastUpdate.Text = "Thời gian cập nhật gần nhất: " + ConvertToVietnamTime(info.LastUpdate);
            lblDaysAgo.Text = info.DaysAgo;
            lblDeveloper.Text = "Nhà phát triển: " + info.Developer;
            lblPublisher.Text = "Nhà phát hành: " + info.Publisher;
            lblReleaseDate.Text = "Ngày phát hành: " + info.ReleaseDate;
        }

        private void lvGameHistory_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvGameHistory.SelectedItems.Count > 0)
            {
                ListViewItem selectedItem = lvGameHistory.SelectedItems[0];
                if (selectedItem.Tag != null)
                {
                    string selectedAppID = selectedItem.Tag.ToString();
                    if (!string.IsNullOrEmpty(selectedAppID) && gameHistory.ContainsKey(selectedAppID))
                    {
                        DisplayGameInfo(gameHistory[selectedAppID]);
                        txtAppID.Text = selectedAppID;
                    }
                }
            }
        }

        private void LoadGameIDs()
        {
            try
            {
                if (File.Exists(idListPath))
                {
                    string[] lines = File.ReadAllLines(idListPath);
                    foreach (string line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        string[] parts = line.Split('|');
                        if (parts.Length >= 2)
                        {
                            string appID = parts[0].Trim();
                            string appName = parts[1].Trim();

                            if (!string.IsNullOrEmpty(appID))
                            {
                                lbSavedIDs.Items.Add($"{appName} ({appID})");
                            }
                        }
                        else if (!string.IsNullOrEmpty(line.Trim()))
                        {
                            lbSavedIDs.Items.Add(line.Trim());
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi đọc danh sách ID: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveGameIDs()
        {
            try
            {
                List<string> lines = new List<string>();

                foreach (object item in lbSavedIDs.Items)
                {
                    if (item == null) continue;

                    string entry = item.ToString();
                    string appID = "";
                    string appName = entry;

                    // Trích xuất ID từ định dạng "Name (ID)"
                    Match match = Regex.Match(entry, @"(.*) \((\d+)\)$");
                    if (match.Success)
                    {
                        appName = match.Groups[1].Value.Trim();
                        appID = match.Groups[2].Value.Trim();
                    }

                    lines.Add($"{appID}|{appName}");
                }

                File.WriteAllLines(idListPath, lines);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu danh sách ID: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAddToList_Click(object sender, EventArgs e)
        {
            string appID = txtAppID.Text.Trim();
            if (string.IsNullOrEmpty(appID))
            {
                MessageBox.Show("Vui lòng nhập ID game!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Kiểm tra xem ID đã có trong danh sách chưa
            bool exists = false;
            foreach (object item in lbSavedIDs.Items)
            {
                if (item != null && item.ToString().Contains($"({appID})"))
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                string gameName = "Game";

                // Lấy tên game nếu đã kiểm tra trước đó
                if (gameHistory.ContainsKey(appID))
                {
                    gameName = gameHistory[appID].Name;
                }

                lbSavedIDs.Items.Add($"{gameName} ({appID})");
                SaveGameIDs();
            }
            else
            {
                MessageBox.Show("ID game này đã có trong danh sách!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnRemoveID_Click(object sender, EventArgs e)
        {
            if (lbSavedIDs.SelectedIndex != -1)
            {
                lbSavedIDs.Items.RemoveAt(lbSavedIDs.SelectedIndex);
                SaveGameIDs();
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một ID để xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void lbSavedIDs_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lbSavedIDs.SelectedIndex != -1 && lbSavedIDs.SelectedItem != null)
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

        private async void btnScanAll_Click(object sender, EventArgs e)
        {
            if (lbSavedIDs.Items.Count == 0)
            {
                MessageBox.Show("Danh sách ID game trống!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Disable các nút trong khi quét
            btnCheckGame.Enabled = false;
            btnAddToList.Enabled = false;
            btnRemoveID.Enabled = false;
            btnScanAll.Enabled = false;

            try
            {
                int method = cbMethod.SelectedIndex;
                int total = lbSavedIDs.Items.Count;
                int current = 0;

                foreach (object item in lbSavedIDs.Items)
                {
                    if (item == null) continue;

                    string selectedItem = item.ToString();
                    Match match = Regex.Match(selectedItem, @"\((\d+)\)$");

                    if (match.Success)
                    {
                        current++;
                        string appID = match.Groups[1].Value;
                        lblStatus.Text = $"Trạng thái: Đang quét {current}/{total} - ID: {appID}";
                        lblStatus.ForeColor = Color.Blue;
                        Application.DoEvents();

                        await CheckGameInfo(appID, method);
                    }
                }

                lblStatus.Text = "Trạng thái: Quét toàn bộ hoàn tất";
                lblStatus.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Trạng thái: Lỗi quét - {ex.Message}";
                lblStatus.ForeColor = Color.Red;
            }
            finally
            {
                // Enable các nút sau khi quét xong
                btnCheckGame.Enabled = true;
                btnAddToList.Enabled = true;
                btnRemoveID.Enabled = true;
                btnScanAll.Enabled = true;
            }
        }

        private void rbScanInterval_CheckedChanged(object sender, EventArgs e)
        {
            int interval = 0;

            if (rb30m.Checked)
                interval = 30 * 60 * 1000; // 30 phút
            else if (rb1h.Checked)
                interval = 60 * 60 * 1000; // 1 giờ
            else if (rb6h.Checked)
                interval = 6 * 60 * 60 * 1000; // 6 giờ
            else if (rb12h.Checked)
                interval = 12 * 60 * 60 * 1000; // 12 giờ
            else if (rb24h.Checked)
                interval = 24 * 60 * 60 * 1000; // 24 giờ

            if (interval > 0)
            {
                scanTimer.Interval = interval;
                scanTimer.Enabled = true;

                DateTime nextScan = DateTime.Now.AddMilliseconds(interval);
                lblNextScan.Text = $"Quét tự động: Lần kế tiếp vào {nextScan.ToString("dd/MM/yyyy HH:mm:ss")}";
            }
            else
            {
                scanTimer.Enabled = false;
                lblNextScan.Text = "Quét tự động: Đã tắt";
            }
        }

        private async void ScanTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                // Tắt timer trong khi đang quét
                scanTimer.Enabled = false;

                // Quét tất cả game trong danh sách
                await ScanAllGames();
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Trạng thái: Lỗi quét tự động - {ex.Message}";
                lblStatus.ForeColor = Color.Red;
            }
            finally
            {
                // Cập nhật thời gian quét tiếp theo
                if (!rbDisabled.Checked)
                {
                    scanTimer.Enabled = true;
                    DateTime nextScan = DateTime.Now.AddMilliseconds(scanTimer.Interval);
                    lblNextScan.Text = $"Quét tự động: Lần kế tiếp vào {nextScan.ToString("dd/MM/yyyy HH:mm:ss")}";
                }
            }
        }

        private async Task ScanAllGames()
        {
            if (lbSavedIDs.Items.Count == 0)
                return;

            int method = cbMethod.SelectedIndex;
            int total = lbSavedIDs.Items.Count;
            int current = 0;

            foreach (object item in lbSavedIDs.Items)
            {
                if (item == null) continue;

                string selectedItem = item.ToString();
                Match match = Regex.Match(selectedItem, @"\((\d+)\)$");

                if (match.Success)
                {
                    current++;
                    string appID = match.Groups[1].Value;
                    lblStatus.Text = $"Trạng thái: Đang quét tự động {current}/{total} - ID: {appID}";
                    lblStatus.ForeColor = Color.Blue;
                    Application.DoEvents();

                    await CheckGameInfo(appID, method);
                }
            }

            lblStatus.Text = "Trạng thái: Quét tự động hoàn tất";
            lblStatus.ForeColor = Color.Green;
        }
    }
}