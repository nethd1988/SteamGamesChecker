using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using IOFile = System.IO.File;

namespace SteamGamesChecker
{
    public partial class TelegramConfigForm : Form
    {
        private TelegramNotifier telegramNotifier;

        public TelegramConfigForm()
        {
            InitializeComponent();
            telegramNotifier = TelegramNotifier.Instance;
        }

        private void TelegramConfigForm_Load(object sender, EventArgs e)
        {
            // Lấy cấu hình hiện tại
            txtBotToken.Text = GetMaskedToken();
            chkEnable.Checked = telegramNotifier.IsEnabled;
            numNotificationDays.Value = telegramNotifier.NotificationThreshold;
            chkSendIcon.Checked = telegramNotifier.SendIcon;

            // Lấy danh sách chat ID
            RefreshChatIdList();

            // Cập nhật UI dựa trên trạng thái hiện tại
            UpdateUIState();

            // Thêm sự kiện SelectedIndexChanged cho lstChatIds
            this.lstChatIds.SelectedIndexChanged += new System.EventHandler(this.lstChatIds_SelectedIndexChanged);
        }

        // Cập nhật trạng thái các điều khiển UI
        private void UpdateUIState()
        {
            bool hasToken = !string.IsNullOrEmpty(telegramNotifier.GetBotToken());
            bool hasChatIds = lstChatIds.Items.Count > 0;

            // Chỉ cho phép bật thông báo khi có token và ít nhất một chat ID
            chkEnable.Enabled = hasToken && hasChatIds;

            // Chỉ cho phép kiểm tra khi có token và chọn chat ID
            btnTestBot.Enabled = hasToken && lstChatIds.SelectedIndex >= 0;

            // Hiển thị trạng thái kết nối
            if (hasToken)
            {
                lblTestStatus.Text = hasChatIds ? "Sẵn sàng" : "Chưa có Chat ID";
                lblTestStatus.ForeColor = hasChatIds ? Color.Green : Color.Orange;
            }
            else
            {
                lblTestStatus.Text = "Chưa cấu hình";
                lblTestStatus.ForeColor = Color.Red;
            }
        }

        // Hiển thị token dưới dạng che giấu một phần
        private string GetMaskedToken()
        {
            string token = telegramNotifier.GetBotToken();
            if (string.IsNullOrEmpty(token) || token.Length < 20)
                return token;

            // Hiển thị 10 ký tự đầu và 5 ký tự cuối, còn lại dùng dấu *
            return token.Substring(0, 10) + new string('*', token.Length - 15) + token.Substring(token.Length - 5);
        }

        private void RefreshChatIdList()
        {
            lstChatIds.Items.Clear();
            foreach (long chatId in telegramNotifier.GetChatIds())
            {
                lstChatIds.Items.Add(chatId.ToString());
            }

            // Cập nhật trạng thái UI sau khi làm mới danh sách
            UpdateUIState();
        }

        private void btnSaveToken_Click(object sender, EventArgs e)
        {
            string token = txtBotToken.Text.Trim();
            if (string.IsNullOrEmpty(token))
            {
                MessageBox.Show("Vui lòng nhập token bot!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Kiểm tra định dạng token (thông thường bắt đầu bằng số và dấu :)
            if (!token.Contains(":"))
            {
                MessageBox.Show("Token bot không đúng định dạng!\nToken thường có dạng: 1234567890:ABCDEF...", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (telegramNotifier.SetBotToken(token))
            {
                MessageBox.Show("Đã lưu token bot thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                txtBotToken.Text = GetMaskedToken();
                // Cập nhật UI sau khi lưu token
                UpdateUIState();
            }
            else
            {
                MessageBox.Show("Lỗi khi thiết lập token bot!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAddChatId_Click(object sender, EventArgs e)
        {
            string chatIdText = txtChatId.Text.Trim();
            if (string.IsNullOrEmpty(chatIdText))
            {
                MessageBox.Show("Vui lòng nhập Chat ID!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!long.TryParse(chatIdText, out long chatId))
            {
                MessageBox.Show("Chat ID phải là số!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Kiểm tra xem Chat ID đã tồn tại chưa
            foreach (var item in lstChatIds.Items)
            {
                if (item.ToString() == chatIdText)
                {
                    MessageBox.Show("Chat ID này đã tồn tại trong danh sách!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }

            telegramNotifier.AddChatId(chatId);
            RefreshChatIdList();
            txtChatId.Clear();

            // Tự động chọn Chat ID mới thêm vào
            for (int i = 0; i < lstChatIds.Items.Count; i++)
            {
                if (lstChatIds.Items[i].ToString() == chatIdText)
                {
                    lstChatIds.SelectedIndex = i;
                    break;
                }
            }
        }

        private void btnRemoveChatId_Click(object sender, EventArgs e)
        {
            if (lstChatIds.SelectedIndex == -1)
            {
                MessageBox.Show("Vui lòng chọn một Chat ID để xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string selectedChatId = lstChatIds.SelectedItem.ToString();
            if (long.TryParse(selectedChatId, out long chatId))
            {
                // Xác nhận trước khi xóa
                DialogResult result = MessageBox.Show(
                    $"Bạn có chắc chắn muốn xóa Chat ID {selectedChatId} khỏi danh sách không?",
                    "Xác nhận xóa",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    telegramNotifier.RemoveChatId(chatId);
                    RefreshChatIdList();
                }
            }
        }

        private async void btnTestBot_Click(object sender, EventArgs e)
        {
            btnTestBot.Enabled = false;
            lblTestStatus.Text = "Đang kiểm tra...";
            lblTestStatus.ForeColor = Color.Blue;

            try
            {
                if (lstChatIds.SelectedIndex == -1 && lstChatIds.Items.Count > 0)
                {
                    lstChatIds.SelectedIndex = 0;
                }

                if (lstChatIds.SelectedIndex == -1)
                {
                    MessageBox.Show("Vui lòng thêm ít nhất một Chat ID!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    lblTestStatus.Text = "Chưa có Chat ID";
                    lblTestStatus.ForeColor = Color.Orange;
                    btnTestBot.Enabled = true;
                    return;
                }

                string selectedChatId = lstChatIds.SelectedItem.ToString();
                if (long.TryParse(selectedChatId, out long chatId))
                {
                    bool success = await telegramNotifier.SendTestMessage(chatId);
                    if (success)
                    {
                        lblTestStatus.Text = "Gửi thành công!";
                        lblTestStatus.ForeColor = Color.Green;
                        MessageBox.Show($"Đã gửi tin nhắn kiểm tra đến chat ID: {chatId}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        lblTestStatus.Text = "Gửi thất bại!";
                        lblTestStatus.ForeColor = Color.Red;
                    }
                }
            }
            catch (Exception ex)
            {
                lblTestStatus.Text = "Lỗi!";
                lblTestStatus.ForeColor = Color.Red;
                MessageBox.Show($"Lỗi khi gửi tin nhắn kiểm tra: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnTestBot.Enabled = true;
            }
        }

        private void chkEnable_CheckedChanged(object sender, EventArgs e)
        {
            telegramNotifier.IsEnabled = chkEnable.Checked;

            // Hiển thị thông báo trạng thái
            if (chkEnable.Checked)
            {
                lblTestStatus.Text = "Thông báo đang BẬT";
                lblTestStatus.ForeColor = Color.Green;
            }
            else
            {
                lblTestStatus.Text = "Thông báo đang TẮT";
                lblTestStatus.ForeColor = Color.Orange;
            }
        }

        private void chkSendIcon_CheckedChanged(object sender, EventArgs e)
        {
            telegramNotifier.SendIcon = chkSendIcon.Checked;
        }

        private void numNotificationDays_ValueChanged(object sender, EventArgs e)
        {
            telegramNotifier.NotificationThreshold = (int)numNotificationDays.Value;
        }

        private void btnHelp_Click(object sender, EventArgs e)
        {
            string helpText =
                "Hướng dẫn cài đặt Telegram Bot:\n\n" +
                "1. Tạo bot mới bằng cách chat với @BotFather trên Telegram.\n" +
                "2. Gửi lệnh /newbot và làm theo hướng dẫn.\n" +
                "3. BotFather sẽ gửi cho bạn token của bot (dạng 123456789:ABCD...).\n" +
                "4. Sao chép token và dán vào ô Token Bot.\n\n" +
                "Để lấy Chat ID:\n" +
                "1. Chat với bot @userinfobot trên Telegram.\n" +
                "2. Bot sẽ gửi cho bạn ID của bạn.\n" +
                "3. Sao chép ID và dán vào ô Chat ID.\n\n" +
                "Gửi thông báo đến nhóm:\n" +
                "1. Thêm bot vào nhóm Telegram của bạn.\n" +
                "2. Đặt bot làm quản trị viên (để có thể gửi tin nhắn).\n" +
                "3. Gửi lệnh /chatid vào nhóm nếu bot hỗ trợ lệnh này, hoặc:\n" +
                "4. Sử dụng bot @RawDataBot để lấy chat ID của nhóm.\n\n" +
                "Lưu ý: Bạn có thể thêm nhiều Chat ID để gửi thông báo đến nhiều người dùng hoặc nhóm khác nhau.";

            MessageBox.Show(helpText, "Hướng dẫn cài đặt Telegram Bot", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void lstChatIds_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Cập nhật trạng thái nút kiểm tra khi chọn chat ID
            btnTestBot.Enabled = lstChatIds.SelectedIndex >= 0;
        }
    }
}