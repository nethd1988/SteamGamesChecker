using System;
using System.Windows.Forms;

namespace SteamGamesChecker
{
    // Phần mở rộng của lớp MainForm
    partial class MainForm
    {
        // Sửa lỗi không tìm thấy phương thức cho các sự kiện Click

        /// <summary>
        /// Phương thức xử lý sự kiện btnCheckUpdate.Click
        /// </summary>
        private void btnCheckUpdate_Click(object sender, EventArgs e)
        {
            string appID = txtAppID.Text.Trim();
            if (string.IsNullOrEmpty(appID))
            {
                MessageBox.Show("Vui lòng nhập App ID!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            lblStatus.Text = "Trạng thái: Đang kiểm tra...";
            lblStatus.ForeColor = System.Drawing.Color.Blue;
            Application.DoEvents();

            try
            {
                // Gọi phương thức TryAllMethods với await, cần bọc trong Task.Run
                System.Threading.Tasks.Task.Run(async () => {
                    var gameInfo = await TryAllMethods(appID);

                    // Phần còn lại của mã được thực hiện sau khi gameInfo đã được tải
                    if (this.InvokeRequired)
                    {
                        this.Invoke(new Action(() => ProcessGameInfo(gameInfo, appID)));
                    }
                    else
                    {
                        ProcessGameInfo(gameInfo, appID);
                    }
                });
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Trạng thái: Lỗi - {ex.Message}";
                lblStatus.ForeColor = System.Drawing.Color.Red;
                System.Diagnostics.Debug.WriteLine($"Lỗi khi kiểm tra cập nhật: {ex.Message}");
            }
        }

        // Phương thức hỗ trợ để xử lý thông tin game
        private void ProcessGameInfo(GameInfo gameInfo, string appID)
        {
            if (gameInfo != null && !string.IsNullOrEmpty(gameInfo.Name) && gameInfo.Name != "Không xác định")
            {
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
                        lvItem.BackColor = System.Drawing.Color.LightGreen;
                    }

                    lvGameHistory.Items.Add(lvItem);

                    // Hiển thị thông báo thành công
                    lblStatus.Text = $"Trạng thái: Đã kiểm tra {gameInfo.Name} - Cập nhật: {gameInfo.LastUpdate}";
                    lblStatus.ForeColor = System.Drawing.Color.Green;
                }
                else
                {
                    gameHistory[appID] = gameInfo;
                    UpdateListViewItem(gameInfo);

                    // Hiển thị thông báo thành công
                    lblStatus.Text = $"Trạng thái: Đã cập nhật {gameInfo.Name} - Cập nhật: {gameInfo.LastUpdate}";
                    lblStatus.ForeColor = System.Drawing.Color.Green;
                }
            }
            else
            {
                lblStatus.Text = "Trạng thái: Không tìm thấy thông tin game";
                lblStatus.ForeColor = System.Drawing.Color.Red;
            }
        }

        /// <summary>
        /// Phương thức xử lý sự kiện btnSave.Click
        /// </summary>
        private void btnSave_Click(object sender, EventArgs e)
        {
            string appID = txtAppID.Text.Trim();
            if (string.IsNullOrEmpty(appID))
            {
                MessageBox.Show("Vui lòng nhập App ID!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Lấy thông tin game từ gameHistory hoặc ListView
            string gameName = "Game " + appID;
            foreach (ListViewItem item in lvGameHistory.Items)
            {
                if (item.SubItems[1].Text == appID)
                {
                    gameName = item.SubItems[0].Text;
                    break;
                }
            }

            // Kiểm tra xem game đã tồn tại trong danh sách chưa
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

        /// <summary>
        /// Phương thức xử lý sự kiện btnRemove.Click
        /// </summary>
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

        /// <summary>
        /// Phương thức xử lý sự kiện btnAutoScan.Click
        /// </summary>
        private void btnAutoScan_Click(object sender, EventArgs e)
        {
            if (scanTimer.Enabled)
            {
                scanTimer.Stop();
                btnAutoScan.Text = "Tự Động Quét";
                lblStatus.Text = "Trạng thái: Tự động quét đã dừng";
                lblStatus.ForeColor = System.Drawing.SystemColors.ControlText;
            }
            else
            {
                // Lấy thời gian quét từ textbox
                if (int.TryParse(txtScanInterval.Text, out int minutes) && minutes > 0)
                {
                    scanTimer.Interval = minutes * 60 * 1000; // Chuyển phút thành mili giây
                    scanTimer.Start();
                    btnAutoScan.Text = "Dừng Tự Động";
                    lblStatus.Text = $"Trạng thái: Tự động quét mỗi {minutes} phút";
                    lblStatus.ForeColor = System.Drawing.Color.Green;

                    // Quét lần đầu ngay sau khi bật
                    System.Threading.Tasks.Task.Run(async () => {
                        await ScanAllGames();
                    });
                }
                else
                {
                    MessageBox.Show("Vui lòng nhập thời gian quét hợp lệ (phút)!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Phương thức xử lý sự kiện btnScanAll.Click
        /// </summary>
        private void btnScanAll_Click(object sender, EventArgs e)
        {
            // Chạy phương thức ScanAllGames bất đồng bộ
            System.Threading.Tasks.Task.Run(async () => {
                await ScanAllGames();
            });
        }

        /// <summary>
        /// Phương thức xử lý sự kiện btnConfigTelegram.Click
        /// </summary>
        private void btnConfigTelegram_Click(object sender, EventArgs e)
        {
            ShowTelegramConfigForm();
        }

        /// <summary>
        /// Phương thức xử lý sự kiện InvokeRequired của ToolStripProgressBar
        /// </summary>
        private void InvokeRequired_Click(object sender, EventArgs e)
        {
            // Phương thức này chỉ để định nghĩa, không thực hiện chức năng gì
        }

        /// <summary>
        /// Phương thức xử lý sự kiện Invoke của ToolStripProgressBar
        /// </summary>
        private void Invoke_Click(object sender, EventArgs e)
        {
            // Phương thức này chỉ để định nghĩa, không thực hiện chức năng gì
        }
    }
}