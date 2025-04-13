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

        // ... các phương thức khác không thay đổi ...
    }
}