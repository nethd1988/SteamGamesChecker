using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;

namespace SteamGamesChecker
{
    public partial class RemoteClientForm : Form
    {
        private List<RemoteClient> clients = new List<RemoteClient>();
        private string configPath = "remote_clients.json";
        private RemoteUpdateService currentService;

        public RemoteClientForm()
        {
            InitializeComponent();
        }

        private void RemoteClientForm_Load(object sender, EventArgs e)
        {
            LoadClients();
            UpdateClientsList();
        }

        private void LoadClients()
        {
            try
            {
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath);
                    var loadedClients = JsonConvert.DeserializeObject<List<RemoteClient>>(json);
                    if (loadedClients != null)
                    {
                        clients = loadedClients;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi đọc danh sách client: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                clients = new List<RemoteClient>();
            }
        }

        private void SaveClients()
        {
            try
            {
                string json = JsonConvert.SerializeObject(clients, Formatting.Indented);
                File.WriteAllText(configPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi lưu danh sách client: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateClientsList()
        {
            lstClients.Items.Clear();
            foreach (var client in clients)
            {
                lstClients.Items.Add($"{client.Name} [{client.ApiUrl}]");
            }
        }

        private async void btnTestConnection_Click(object sender, EventArgs e)
        {
            if (lstClients.SelectedIndex < 0)
            {
                MessageBox.Show("Vui lòng chọn một client để kiểm tra kết nối!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int selectedIndex = lstClients.SelectedIndex;
            var client = clients[selectedIndex];

            lblStatus.Text = "Đang kiểm tra kết nối...";
            lblStatus.ForeColor = Color.Blue;
            btnTestConnection.Enabled = false;

            try
            {
                currentService = new RemoteUpdateService(client.ApiUrl, client.ApiKey);
                var (success, message) = await currentService.TestConnectionAsync();

                if (success)
                {
                    lblStatus.Text = message;
                    lblStatus.ForeColor = Color.Green;
                }
                else
                {
                    lblStatus.Text = message;
                    lblStatus.ForeColor = Color.Red;
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Lỗi: {ex.Message}";
                lblStatus.ForeColor = Color.Red;
            }
            finally
            {
                btnTestConnection.Enabled = true;
            }
        }

        private async void btnSendCommand_Click(object sender, EventArgs e)
        {
            if (lstClients.SelectedIndex < 0)
            {
                MessageBox.Show("Vui lòng chọn một client để gửi lệnh!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtAppId.Text))
            {
                MessageBox.Show("Vui lòng nhập App ID!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int selectedIndex = lstClients.SelectedIndex;
            var client = clients[selectedIndex];
            string appId = txtAppId.Text.Trim();
            string reason = txtReason.Text.Trim();
            if (string.IsNullOrEmpty(reason))
            {
                reason = $"Yêu cầu cập nhật từ SteamGamesChecker ({DateTime.Now:dd/MM/yyyy HH:mm:ss})";
            }

            lblStatus.Text = "Đang gửi lệnh...";
            lblStatus.ForeColor = Color.Blue;
            btnSendCommand.Enabled = false;

            try
            {
                currentService = new RemoteUpdateService(client.ApiUrl, client.ApiKey);
                bool success = await currentService.SendUpdateCommandAsync(appId, reason);

                if (success)
                {
                    lblStatus.Text = $"Đã gửi lệnh cập nhật AppID {appId} thành công!";
                    lblStatus.ForeColor = Color.Green;
                }
                else
                {
                    lblStatus.Text = $"Lỗi khi gửi lệnh cập nhật AppID {appId}";
                    lblStatus.ForeColor = Color.Red;
                }
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Lỗi: {ex.Message}";
                lblStatus.ForeColor = Color.Red;
            }
            finally
            {
                btnSendCommand.Enabled = true;
            }
        }

        private void btnAddClient_Click(object sender, EventArgs e)
        {
            using (var form = new Form())
            {
                form.Text = "Thêm Client Mới";
                form.Size = new Size(400, 250);
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                var lblName = new Label { Text = "Tên client:", Left = 20, Top = 20, Width = 120 };
                var txtName = new TextBox { Left = 150, Top = 20, Width = 200 };

                var lblUrl = new Label { Text = "URL API:", Left = 20, Top = 50, Width = 120 };
                var txtUrl = new TextBox { Text = "http://", Left = 150, Top = 50, Width = 200 };

                var lblApiKey = new Label { Text = "API Key:", Left = 20, Top = 80, Width = 120 };
                var txtApiKey = new TextBox { Left = 150, Top = 80, Width = 200 };

                var btnSave = new Button { Text = "Lưu", Left = 150, Top = 150, Width = 90 };
                var btnCancel = new Button { Text = "Hủy", Left = 260, Top = 150, Width = 90 };

                btnSave.Click += (s, ev) =>
                {
                    if (string.IsNullOrWhiteSpace(txtName.Text) ||
                        string.IsNullOrWhiteSpace(txtUrl.Text) ||
                        string.IsNullOrWhiteSpace(txtApiKey.Text))
                    {
                        MessageBox.Show("Vui lòng nhập đầy đủ thông tin!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    clients.Add(new RemoteClient
                    {
                        Name = txtName.Text.Trim(),
                        ApiUrl = txtUrl.Text.Trim(),
                        ApiKey = txtApiKey.Text.Trim()
                    });

                    SaveClients();
                    UpdateClientsList();
                    form.DialogResult = DialogResult.OK;
                };

                btnCancel.Click += (s, ev) => form.DialogResult = DialogResult.Cancel;

                form.Controls.AddRange(new Control[]
                {
                    lblName, txtName,
                    lblUrl, txtUrl,
                    lblApiKey, txtApiKey,
                    btnSave, btnCancel
                });

                form.ShowDialog();
            }
        }

        private void btnEditClient_Click(object sender, EventArgs e)
        {
            if (lstClients.SelectedIndex < 0)
            {
                MessageBox.Show("Vui lòng chọn một client để sửa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int selectedIndex = lstClients.SelectedIndex;
            var client = clients[selectedIndex];

            using (var form = new Form())
            {
                form.Text = "Sửa Client";
                form.Size = new Size(400, 250);
                form.StartPosition = FormStartPosition.CenterParent;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.MaximizeBox = false;
                form.MinimizeBox = false;

                var lblName = new Label { Text = "Tên client:", Left = 20, Top = 20, Width = 120 };
                var txtName = new TextBox { Text = client.Name, Left = 150, Top = 20, Width = 200 };

                var lblUrl = new Label { Text = "URL API:", Left = 20, Top = 50, Width = 120 };
                var txtUrl = new TextBox { Text = client.ApiUrl, Left = 150, Top = 50, Width = 200 };

                var lblApiKey = new Label { Text = "API Key:", Left = 20, Top = 80, Width = 120 };
                var txtApiKey = new TextBox { Text = client.ApiKey, Left = 150, Top = 80, Width = 200 };

                var btnSave = new Button { Text = "Lưu", Left = 150, Top = 150, Width = 90 };
                var btnCancel = new Button { Text = "Hủy", Left = 260, Top = 150, Width = 90 };

                btnSave.Click += (s, ev) =>
                {
                    if (string.IsNullOrWhiteSpace(txtName.Text) ||
                        string.IsNullOrWhiteSpace(txtUrl.Text) ||
                        string.IsNullOrWhiteSpace(txtApiKey.Text))
                    {
                        MessageBox.Show("Vui lòng nhập đầy đủ thông tin!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    client.Name = txtName.Text.Trim();
                    client.ApiUrl = txtUrl.Text.Trim();
                    client.ApiKey = txtApiKey.Text.Trim();

                    SaveClients();
                    UpdateClientsList();
                    form.DialogResult = DialogResult.OK;
                };

                btnCancel.Click += (s, ev) => form.DialogResult = DialogResult.Cancel;

                form.Controls.AddRange(new Control[]
                {
                    lblName, txtName,
                    lblUrl, txtUrl,
                    lblApiKey, txtApiKey,
                    btnSave, btnCancel
                });

                form.ShowDialog();
            }
        }

        private void btnDeleteClient_Click(object sender, EventArgs e)
        {
            if (lstClients.SelectedIndex < 0)
            {
                MessageBox.Show("Vui lòng chọn một client để xóa!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            int selectedIndex = lstClients.SelectedIndex;
            var client = clients[selectedIndex];

            DialogResult result = MessageBox.Show(
                $"Bạn có chắc chắn muốn xóa client '{client.Name}'?",
                "Xác nhận xóa",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                clients.RemoveAt(selectedIndex);
                SaveClients();
                UpdateClientsList();
                lblStatus.Text = "Đã xóa client";
            }
        }

        private void lstClients_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnEditClient.Enabled = lstClients.SelectedIndex >= 0;
            btnDeleteClient.Enabled = lstClients.SelectedIndex >= 0;
            btnTestConnection.Enabled = lstClients.SelectedIndex >= 0;
            btnSendCommand.Enabled = lstClients.SelectedIndex >= 0 && !string.IsNullOrWhiteSpace(txtAppId.Text);
        }

        private void txtAppId_TextChanged(object sender, EventArgs e)
        {
            btnSendCommand.Enabled = lstClients.SelectedIndex >= 0 && !string.IsNullOrWhiteSpace(txtAppId.Text);
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}