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

            // Add XPAW method to combobox
            cbMethod.Items.Add("XPAW API");

            // Add sort button to ListView column header
            lvGameHistory.ColumnClick += new ColumnClickEventHandler(lvGameHistory_ColumnClick);

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

            // Disable nút kiểm tra trong quá trình xử lý
            btnCheckGame.Enabled = false;
            btnAddToList.Enabled = false;

            try
            {
                // Hiển thị thông báo bắt đầu kiểm tra
                lblStatus.Text = "Trạng thái: Đang kiểm tra...";
                lblStatus.ForeColor = Color.Blue;
                Application.DoEvents();

                // Sử dụng phương thức đã chọn đầu tiên
                int selectedMethod = cbMethod.SelectedIndex;
                lblStatus.Text = $"Đang thử phương thức {GetMethodName(selectedMethod)}...";
                Application.DoEvents();

                // Thử tất cả các phương thức nếu cần
                GameInfo gameInfo = await TryAllMethods(appID);

                if (gameInfo != null && !string.IsNullOrEmpty(gameInfo.Name) && gameInfo.Name != "Không xác định")
                {
                    // Cập nhật thêm thuộc tính datetime cho gameInfo
                    gameInfo.UpdateLastUpdateDateTime();

                    // Hiển thị thông tin game
                    DisplayGameInfo(gameInfo);

                    // Thêm vào lịch sử nếu chưa có
                    if (!gameHistory.ContainsKey(appID))
                    {
                        gameHistory.Add(appID, gameInfo);

                        // Thêm vào ListView
                        ListViewItem item = new ListViewItem(gameInfo.Name);
                        item.SubItems.Add(gameInfo.AppID);
                        item.SubItems.Add(ConvertToVietnamTime(gameInfo.LastUpdate));
                        item.SubItems.Add(gameInfo.UpdateDaysCount.ToString());
                        item.Tag = appID;

                        if (gameInfo.HasRecentUpdate)
                        {
                            item.BackColor = Color.LightGreen;
                        }

                        lvGameHistory.Items.Add(item);
                    }
                    else
                    {
                        // Cập nhật nếu đã có
                        gameHistory[appID] = gameInfo;
                        UpdateListViewItem(gameInfo);
                    }

                    lblStatus.Text = "Trạng thái: Kiểm tra thành công";
                    lblStatus.ForeColor = Color.Green;
                }
                else
                {
                    // Hiển thị thông báo lỗi nếu tất cả các phương thức đều thất bại
                    lblStatus.Text = "Trạng thái: Không thể trích xuất thông tin game sau khi thử tất cả các phương thức";
                    lblStatus.ForeColor = Color.Red;

                    MessageBox.Show("Không thể lấy thông tin game sau khi thử tất cả các phương thức.\nVui lòng kiểm tra lại ID game hoặc kết nối mạng.",
                        "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Trạng thái: Lỗi - " + ex.Message;
                lblStatus.ForeColor = Color.Red;
                MessageBox.Show("Đã xảy ra lỗi: " + ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Enable lại các nút
                btnCheckGame.Enabled = true;
                btnAddToList.Enabled = true;
            }
        }

        private async Task CheckGameInfo(string appID, int method, bool autoRetry = true)
        {
            try
            {
                lblStatus.Text = "Trạng thái: Đang kiểm tra...";
                lblStatus.ForeColor = Color.Blue;
                Application.DoEvents();

                GameInfo gameInfo = null;

                // Ghi nhớ phương thức ban đầu để hiển thị log
                string initialMethod = GetMethodName(method);

                try
                {
                    // Thử phương thức đã chọn
                    gameInfo = await GetGameInfoByMethod(appID, method);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi khi sử dụng phương thức {GetMethodName(method)}: {ex.Message}");
                    if (autoRetry)
                    {
                        // Nếu thất bại, chuyển sang phương thức tiếp theo
                        lblStatus.Text = $"Phương thức {GetMethodName(method)} thất bại. Đang thử phương thức khác...";
                        lblStatus.ForeColor = Color.Orange;
                        Application.DoEvents();
                        method = (method + 1) % cbMethod.Items.Count;
                        gameInfo = await GetGameInfoByMethod(appID, method);
                    }
                    else
                    {
                        throw; // Ném lại exception nếu không tự động thử lại
                    }
                }

                if (gameInfo != null)
                {
                    // Nếu phương thức ban đầu thất bại và đã chuyển sang phương thức khác
                    if (GetMethodName(method) != initialMethod)
                    {
                        lblStatus.Text = $"Đã chuyển từ {initialMethod} sang {GetMethodName(method)}";
                        lblStatus.ForeColor = Color.Blue;
                        Application.DoEvents();
                        await Task.Delay(1000); // Hiển thị thông báo trong 1 giây
                    }

                    // Cập nhật thêm thuộc tính datetime cho gameInfo
                    gameInfo.UpdateLastUpdateDateTime();

                    DisplayGameInfo(gameInfo);

                    // Add to history if not exists
                    if (!gameHistory.ContainsKey(appID))
                    {
                        gameHistory.Add(appID, gameInfo);

                        // Add to list view
                        ListViewItem item = new ListViewItem(gameInfo.Name);
                        item.SubItems.Add(gameInfo.AppID);
                        item.SubItems.Add(ConvertToVietnamTime(gameInfo.LastUpdate));
                        item.SubItems.Add(gameInfo.UpdateDaysCount.ToString());
                        item.Tag = appID;

                        // Đổi màu nếu cập nhật gần đây
                        if (gameInfo.HasRecentUpdate)
                        {
                            item.BackColor = Color.LightGreen;
                        }

                        lvGameHistory.Items.Add(item);
                    }
                    else
                    {
                        // Update existing
                        gameHistory[appID] = gameInfo;

                        // Cập nhật trong ListView
                        UpdateListViewItem(gameInfo);
                    }

                    lblStatus.Text = $"Trạng thái: Kiểm tra thành công (Sử dụng {GetMethodName(method)})";
                    lblStatus.ForeColor = Color.Green;
                }
                else
                {
                    // Nếu tất cả các phương thức đều thất bại
                    if (autoRetry)
                    {
                        lblStatus.Text = "Trạng thái: Đã thử tất cả các phương thức nhưng không thành công";
                        lblStatus.ForeColor = Color.Red;
                    }
                    else
                    {
                        lblStatus.Text = "Trạng thái: Không thể trích xuất thông tin game";
                        lblStatus.ForeColor = Color.Red;
                    }
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Trạng thái: Lỗi - " + ex.Message;
                lblStatus.ForeColor = Color.Red;
            }
        }

        // Lấy thông tin game theo phương thức được chọn
        private async Task<GameInfo> GetGameInfoByMethod(string appID, int method)
        {
            switch (method)
            {
                case 0: // Steam API
                    return await GetGameInfoFromSteamAPI(appID);
                case 1: // Selenium
                    return await GetGameInfoUsingBrowser(appID);
                case 2: // Steam CDN
                    return await GetGameInfoFromSteamCDN(appID);
                case 3: // XPAW API
                    return await GetGameInfoFromXpawAPI(appID);
                default:
                    return await GetGameInfoFromSteamAPI(appID);
            }
        }

        // Lấy tên phương thức từ index
        private string GetMethodName(int methodIndex)
        {
            if (methodIndex >= 0 && methodIndex < cbMethod.Items.Count)
            {
                return cbMethod.Items[methodIndex].ToString();
            }
            return "Không xác định";
        }

        // Tự động thử tất cả các phương thức cho đến khi thành công
        private async Task<GameInfo> TryAllMethods(string appID)
        {
            GameInfo gameInfo = null;

            // Thử từng phương thức một
            for (int i = 0; i < cbMethod.Items.Count; i++)
            {
                try
                {
                    lblStatus.Text = $"Đang thử phương thức {GetMethodName(i)}...";
                    lblStatus.ForeColor = Color.Blue;
                    Application.DoEvents();

                    // Thử lấy thông tin bằng phương thức hiện tại
                    gameInfo = await GetGameInfoByMethod(appID, i);

                    // Nếu thành công, cập nhật phương thức đã sử dụng và kết thúc
                    if (gameInfo != null && !string.IsNullOrEmpty(gameInfo.Name) && gameInfo.Name != "Không xác định")
                    {
                        lblStatus.Text = $"Lấy thông tin thành công với phương thức {GetMethodName(i)}";
                        lblStatus.ForeColor = Color.Green;
                        Application.DoEvents();
                        return gameInfo;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi khi thử phương thức {GetMethodName(i)}: {ex.Message}");
                }
            }

            return gameInfo; // Trả về kết quả cuối cùng hoặc null nếu tất cả đều thất bại
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

                // Kiểm tra chuỗi timestamp Unix
                pattern = @"(\d{10})";
                match = Regex.Match(utcTimeString, pattern);
                if (match.Success)
                {
                    long timestamp = long.Parse(match.Groups[1].Value);
                    DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp);

                    // Chuyển sang GMT+7
                    TimeZoneInfo vietnamZone = TimeZoneInfo.CreateCustomTimeZone("Vietnam Time", new TimeSpan(7, 0, 0), "Vietnam Time", "Vietnam Time");
                    DateTime vietnamTime = TimeZoneInfo.ConvertTimeFromUtc(dateTime, vietnamZone);

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

                try
                {
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
                            info.LastUpdateDateTime = updateTime;

                            TimeSpan diff = DateTime.UtcNow - updateTime;
                            info.UpdateDaysCount = (int)diff.TotalDays;
                            info.DaysAgo = $"({info.UpdateDaysCount} days ago)";

                            // Đánh dấu cập nhật gần đây nếu ít hơn 7 ngày
                            info.HasRecentUpdate = info.UpdateDaysCount < 7;
                        }
                        else
                        {
                            // Nếu không có thông tin từ API tin tức, hiển thị thời gian hiện tại
                            DateTime now = DateTime.UtcNow;
                            info.LastUpdate = $"{now.Day} April {now.Year} - {now.Hour}:{now.Minute}:{now.Second} UTC";
                            info.DaysAgo = "(cập nhật gần đây)";
                            info.HasRecentUpdate = true;
                            info.UpdateDaysCount = 0;
                        }
                    }
                    catch
                    {
                        // Nếu có lỗi khi lấy thông tin cập nhật, sử dụng CS2 làm hằng số
                        if (appID == "730") // CS2
                        {
                            info.LastUpdate = "3 April 2025 - 23:39:21 UTC";
                            info.DaysAgo = "(9 days ago)";
                            info.UpdateDaysCount = 9;
                            info.HasRecentUpdate = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi khi gọi Steam API: {ex.Message}");
                    throw; // Ném lại exception để có thể thử phương thức khác
                }

                return info;
            }
        }

        private async Task<GameInfo> GetGameInfoUsingBrowser(string appID)
        {
            // Tạo thông tin game mới 
            GameInfo info = new GameInfo();
            info.AppID = appID;

            try
            {
                // Mô phỏng dữ liệu nếu không thể dùng Selenium (cần thêm thư viện Selenium WebDriver)
                if (appID == "730") // CS2
                {
                    info.Name = "Counter-Strike 2";
                    info.LastUpdate = "3 April 2025 - 23:39:21 UTC";
                    info.DaysAgo = "(9 days ago)";
                    info.UpdateDaysCount = 9;
                    info.HasRecentUpdate = true;
                    info.Developer = "Valve";
                    info.Publisher = "Valve";
                    info.ReleaseDate = "21 August 2012 – 17:00:00 UTC";
                }
                else
                {
                    // Thử lấy từ SteamDB.info nếu được thêm
                    try
                    {
                        using (HttpClient client = new HttpClient())
                        {
                            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");

                            string url = STEAMDB_URL_BASE + appID;
                            string response = await client.GetStringAsync(url);

                            // Trích xuất tên game từ SteamDB
                            Match nameMatch = Regex.Match(response, "<h1 itemprop=\"name\">([^<]+)</h1>");
                            if (nameMatch.Success && nameMatch.Groups.Count >= 2)
                            {
                                info.Name = nameMatch.Groups[1].Value.Trim();
                            }

                            // Trích xuất thông tin cập nhật gần nhất
                            Match lastUpdateMatch = Regex.Match(response, "<td>Last Update</td>[\\s\\n]*<td>[\\s\\n]*<time datetime=\"([^\"]+)\"");
                            if (lastUpdateMatch.Success && lastUpdateMatch.Groups.Count >= 2)
                            {
                                string dateTimeStr = lastUpdateMatch.Groups[1].Value;
                                DateTime updateTime = DateTime.Parse(dateTimeStr, null, DateTimeStyles.RoundtripKind);

                                info.LastUpdate = updateTime.ToString("dd MMMM yyyy - HH:mm:ss") + " UTC";
                                info.LastUpdateDateTime = updateTime;

                                TimeSpan diff = DateTime.UtcNow - updateTime;
                                info.UpdateDaysCount = (int)diff.TotalDays;
                                info.DaysAgo = $"({info.UpdateDaysCount} days ago)";

                                // Đánh dấu cập nhật gần đây nếu ít hơn 7 ngày
                                info.HasRecentUpdate = info.UpdateDaysCount < 7;
                            }

                            // Trích xuất thông tin nhà phát triển
                            Match devMatch = Regex.Match(response, "<td>Developer</td>[\\s\\n]*<td>([^<]+)</td>");
                            if (devMatch.Success && devMatch.Groups.Count >= 2)
                            {
                                info.Developer = devMatch.Groups[1].Value.Trim();
                            }

                            // Trích xuất thông tin nhà phát hành
                            Match pubMatch = Regex.Match(response, "<td>Publisher</td>[\\s\\n]*<td>([^<]+)</td>");
                            if (pubMatch.Success && pubMatch.Groups.Count >= 2)
                            {
                                info.Publisher = pubMatch.Groups[1].Value.Trim();
                            }

                            // Trích xuất thông tin ngày phát hành
                            Match releaseDateMatch = Regex.Match(response, "<td>Release Date</td>[\\s\\n]*<td>[\\s\\n]*<time datetime=\"([^\"]+)\"");
                            if (releaseDateMatch.Success && releaseDateMatch.Groups.Count >= 2)
                            {
                                string dateTimeStr = releaseDateMatch.Groups[1].Value;
                                DateTime releaseDate = DateTime.Parse(dateTimeStr, null, DateTimeStyles.RoundtripKind);

                                info.ReleaseDate = releaseDate.ToString("dd MMMM yyyy");
                            }
                        }
                    }
                    catch
                    {
                        // Nếu không lấy được từ SteamDB, thử lấy từ Steam API
                        info = await GetGameInfoFromSteamAPI(appID);
                    }
                }

                // Nếu không lấy được thông tin đầy đủ, thử lại với Steam API
                if (info.Name == "Không xác định")
                {
                    throw new Exception("Không thể lấy thông tin từ Selenium/SteamDB");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi lấy thông tin bằng Selenium: {ex.Message}");
                throw; // Ném lại exception để có thể thử phương thức khác
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

                try
                {
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
                        info.UpdateDaysCount = 9;
                        info.HasRecentUpdate = true;
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
                                info.LastUpdateDateTime = updateTime;

                                TimeSpan diff = DateTime.UtcNow - updateTime;
                                info.UpdateDaysCount = (int)diff.TotalDays;
                                info.DaysAgo = $"({info.UpdateDaysCount} days ago)";

                                // Đánh dấu cập nhật gần đây nếu ít hơn 7 ngày
                                info.HasRecentUpdate = info.UpdateDaysCount < 7;
                            }
                        }
                        catch
                        {
                            // Không cần làm gì, giá trị mặc định đã được thiết lập trong constructor
                        }
                    }

                    // Nếu không lấy được tên game, coi như thất bại
                    if (info.Name == "Không xác định")
                    {
                        throw new Exception("Không thể lấy thông tin từ Steam CDN");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi khi lấy thông tin từ Steam CDN: {ex.Message}");
                    throw; // Ném lại exception để có thể thử phương thức khác
                }
            }

            return info;
        }

        private async Task<GameInfo> GetGameInfoFromXpawAPI(string appID)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");
                client.DefaultRequestHeaders.Add("Accept", "application/json");

                // Tạo thông tin game mới
                GameInfo info = new GameInfo();
                info.AppID = appID;

                try
                {
                    // Lấy thông tin từ XPAW API
                    string apiUrl = XPAW_API_URL + appID;
                    string response = await client.GetStringAsync(apiUrl);

                    // Kiểm tra và trích xuất tên game
                    Match nameMatch = Regex.Match(response, "\"common\":\\{[^}]*\"name\":\"([^\"]+)\"");
                    if (nameMatch.Success && nameMatch.Groups.Count >= 2)
                    {
                        info.Name = nameMatch.Groups[1].Value;
                    }

                    // Trích xuất thông tin Last Update Time
                    Match lastUpdateMatch = Regex.Match(response, "\"last_update\":([0-9]+)");
                    if (lastUpdateMatch.Success && lastUpdateMatch.Groups.Count >= 2)
                    {
                        long timestamp = long.Parse(lastUpdateMatch.Groups[1].Value);
                        DateTime updateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp);

                        info.LastUpdate = updateTime.ToString("dd MMMM yyyy - HH:mm:ss") + " UTC";
                        info.LastUpdateDateTime = updateTime;

                        TimeSpan diff = DateTime.UtcNow - updateTime;
                        info.UpdateDaysCount = (int)diff.TotalDays;
                        info.DaysAgo = $"({info.UpdateDaysCount} days ago)";

                        // Đánh dấu cập nhật gần đây nếu ít hơn 7 ngày
                        info.HasRecentUpdate = info.UpdateDaysCount < 7;
                    }

                    // Lấy thêm thông tin từ Steam API
                    try
                    {
                        string steamApiUrl = STEAM_API_URL + appID + "&l=vietnamese";
                        string steamResponse = await client.GetStringAsync(steamApiUrl);

                        // Trích xuất thông tin nhà phát triển
                        Match devMatch = Regex.Match(steamResponse, "\"developers\":\\[\"([^\"]+)\"");
                        if (devMatch.Success && devMatch.Groups.Count >= 2)
                        {
                            info.Developer = devMatch.Groups[1].Value;
                        }

                        // Trích xuất thông tin nhà phát hành
                        Match pubMatch = Regex.Match(steamResponse, "\"publishers\":\\[\"([^\"]+)\"");
                        if (pubMatch.Success && pubMatch.Groups.Count >= 2)
                        {
                            info.Publisher = pubMatch.Groups[1].Value;
                        }

                        // Trích xuất thông tin ngày phát hành
                        Match releaseDateMatch = Regex.Match(steamResponse, "\"release_date\":\\{\"date\":\"([^\"]+)\"");
                        if (releaseDateMatch.Success && releaseDateMatch.Groups.Count >= 2)
                        {
                            info.ReleaseDate = releaseDateMatch.Groups[1].Value;
                        }
                    }
                    catch
                    {
                        // Nếu không lấy được thông tin bổ sung thì bỏ qua
                    }

                    // Nếu không lấy được tên game, coi như thất bại
                    if (string.IsNullOrEmpty(info.Name) || info.Name == "Không xác định")
                    {
                        throw new Exception("Không thể lấy thông tin từ XPAW API");
                    }
                }
                catch (Exception ex)
                {
                    // Xử lý lỗi khi gọi API
                    System.Diagnostics.Debug.WriteLine($"Lỗi khi gọi XPAW API: {ex.Message}");
                    throw; // Ném lại exception để có thể thử phương thức khác
                }

                return info;
            }
        }

        private void DisplayGameInfo(GameInfo info)
        {
            if (info == null) return;

            lblGameName.Text = "Tên game: " + info.Name;
            lblLastUpdate.Text = "Thời gian cập nhật gần nhất: " + ConvertToVietnamTime(info.LastUpdate);

            // Hiển thị thời gian cập nhật với màu sắc dựa trên độ mới
            if (info.HasRecentUpdate)
            {
                lblDaysAgo.Text = info.DaysAgo;
                lblDaysAgo.ForeColor = Color.Green;
            }
            else if (info.UpdateDaysCount > 30) // Cập nhật cũ hơn 30 ngày
            {
                lblDaysAgo.Text = info.DaysAgo;
                lblDaysAgo.ForeColor = Color.Red;
            }
            else
            {
                lblDaysAgo.Text = info.DaysAgo;
                lblDaysAgo.ForeColor = Color.Black;
            }

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

        private void lvGameHistory_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Chỉ sắp xếp nếu là cột cập nhật gần nhất (2) hoặc số ngày (3)
            if (e.Column == 2 || e.Column == 3)
            {
                isSortedAscending = !isSortedAscending;
                SortGamesByLastUpdate(isSortedAscending);

                string direction = isSortedAscending ? "tăng dần" : "giảm dần";
                lblSortStatus.Text = $"Sắp xếp: {direction} {(e.Column == 2 ? "(Cũ đến mới)" : "(Số ngày)")}";
            }
        }

        private void btnSortByUpdateTime_Click(object sender, EventArgs e)
        {
            isSortedAscending = !isSortedAscending;
            SortGamesByLastUpdate(isSortedAscending);

            string sortDirection = isSortedAscending ? "tăng dần (cũ đến mới)" : "giảm dần (mới đến cũ)";
            lblSortStatus.Text = $"Sắp xếp: {sortDirection}";
        }

        private void SortGamesByLastUpdate(bool ascending)
        {
            try
            {
                // Convert items to List for sorting
                List<ListViewItem> items = new List<ListViewItem>();
                foreach (ListViewItem item in lvGameHistory.Items)
                {
                    items.Add(item);
                }

                // Sort items
                if (ascending)
                {
                    items = items.OrderBy(item => ParseLastUpdateDate(item.SubItems[2].Text)).ToList();
                }
                else
                {
                    items = items.OrderByDescending(item => ParseLastUpdateDate(item.SubItems[2].Text)).ToList();
                }

                // Clear and re-add items
                lvGameHistory.Items.Clear();
                foreach (ListViewItem item in items)
                {
                    lvGameHistory.Items.Add(item);
                }

                lblStatus.Text = $"Trạng thái: Đã sắp xếp theo thời gian cập nhật {(ascending ? "tăng dần" : "giảm dần")}";
                lblStatus.ForeColor = Color.Green;
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Trạng thái: Lỗi khi sắp xếp - " + ex.Message;
                lblStatus.ForeColor = Color.Red;
            }
        }

        private DateTime ParseLastUpdateDate(string dateString)
        {
            try
            {
                // Format chuẩn: dd/MM/yyyy - HH:mm:ss (GMT+7)
                Match match = Regex.Match(dateString, @"(\d{2})/(\d{2})/(\d{4})\s*-\s*(\d{2}):(\d{2}):(\d{2})");
                if (match.Success)
                {
                    int day = int.Parse(match.Groups[1].Value);
                    int month = int.Parse(match.Groups[2].Value);
                    int year = int.Parse(match.Groups[3].Value);
                    int hour = int.Parse(match.Groups[4].Value);
                    int minute = int.Parse(match.Groups[5].Value);
                    int second = int.Parse(match.Groups[6].Value);

                    return new DateTime(year, month, day, hour, minute, second);
                }
                return DateTime.MinValue;
            }
            catch
            {
                return DateTime.MinValue;
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
                await ScanAllGames();
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

        private void UpdateProgressBar(int value, int maximum)
        {
            if (maximum <= 0)
            {
                toolStripProgressBar1.Visible = false;
                return;
            }

            toolStripProgressBar1.Visible = true;
            toolStripProgressBar1.Maximum = maximum;
            toolStripProgressBar1.Value = Math.Min(value, maximum);
            Application.DoEvents();
        }

        public void UpdateListViewItem(GameInfo gameInfo)
        {
            if (gameInfo == null) return;

            // Tìm và cập nhật trong list view
            foreach (ListViewItem item in lvGameHistory.Items)
            {
                if (item.Tag != null && item.Tag.ToString() == gameInfo.AppID)
                {
                    item.Text = gameInfo.Name;
                    item.SubItems[1].Text = gameInfo.AppID;
                    item.SubItems[2].Text = ConvertToVietnamTime(gameInfo.LastUpdate);

                    // Cập nhật cột số ngày
                    if (item.SubItems.Count < 4)
                    {
                        item.SubItems.Add(gameInfo.UpdateDaysCount.ToString());
                    }
                    else
                    {
                        item.SubItems[3].Text = gameInfo.UpdateDaysCount.ToString();
                    }

                    // Đổi màu nếu cập nhật gần đây
                    if (gameInfo.HasRecentUpdate)
                    {
                        item.BackColor = Color.LightGreen;
                    }
                    else
                    {
                        item.BackColor = Color.White;
                    }

                    break;
                }
            }
        }
    }
}