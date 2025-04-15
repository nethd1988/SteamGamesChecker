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

            // Thêm menu "Cấu hình Điều khiển Từ xa"
            ToolStripMenuItem remoteMenuItem = new ToolStripMenuItem("Cấu hình Điều khiển Từ xa");
            remoteMenuItem.Click += (s, ev) => ShowRemoteSettingsDialog();
            toolsToolStripMenuItem.DropDownItems.Add(remoteMenuItem);

            // Thêm menu "Quản lý Client Từ Xa"
            ToolStripMenuItem remoteClientsMenuItem = new ToolStripMenuItem("Quản lý Client Từ Xa");
            remoteClientsMenuItem.Click += (s, ev) => ShowRemoteClientsForm();
            toolsToolStripMenuItem.DropDownItems.Add(remoteClientsMenuItem);

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
                    string configPath = "remote_clients.json";
                    if (File.Exists(configPath))
                    {
                        string json = File.ReadAllText(configPath);
                        var clients = JsonConvert.DeserializeObject<List<RemoteClient>>(json);

                        if (clients != null && clients.Count > 0)
                        {
                            int successCount = 0;
                            foreach (var client in clients)
                            {
                                try
                                {
                                    var remoteService = new RemoteUpdateService(client.ApiUrl, client.ApiKey);
                                    bool success = await remoteService.SendUpdateCommandAsync(
                                        gameInfo.AppID,
                                        $"Cập nhật tự động phát hiện lúc {DateTime.Now:dd/MM/yyyy HH:mm:ss}");

                                    if (success)
                                        successCount++;
                                }
                                catch (Exception ex)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Lỗi khi gửi lệnh tới client {client.Name}: {ex.Message}");
                                }
                            }

                            this.Invoke(new Action(() =>
                            {
                                lblStatus.Text += $" - Đã gửi lệnh cập nhật tới {successCount}/{clients.Count} client";
                            }));
                        }
                        else
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
                        }
                    }
                    else
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

        private void ShowRemoteClientsForm()
        {
            using (RemoteClientForm clientForm = new RemoteClientForm())
            {
                clientForm.ShowDialog();
                LoadRemoteUpdateSettings();
                _remoteUpdateService = new RemoteUpdateService(_clientApiUrl, _apiKey);
            }
        }

        public class RemoteClient
        {
            public string Name { get; set; }
            public string ApiUrl { get; set; }
            public string ApiKey { get; set; }
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

        // Các phương thức khác (giả định đã có sẵn)
        private void ScanTimer_Tick(object sender, EventArgs e) { /* Logic quét tự động */ }
        private void LoadConfig() { /* Logic tải cấu hình */ }
        private void SaveConfig() { /* Logic lưu cấu hình */ }
        private void ApplyConfig() { /* Logic áp dụng cấu hình */ }
        private void InitializeImageList() { /* Khởi tạo ImageList */ }
        private void LoadGameIDs() { /* Tải danh sách game ID */ }
        private void UpdateTelegramMenuStatus() { /* Cập nhật trạng thái menu Telegram */ }
        private void LoadGameHistoryToListView() { /* Tải lịch sử game vào ListView */ }
        private void LoadScanHistoryToListView() { /* Tải lịch sử quét vào ListView */ }
        private void SortGamesByLastUpdate(bool ascending) { /* Sắp xếp game theo lần cập nhật cuối */ }
        private void ShowLastScanTime() { /* Hiển thị thời gian quét cuối */ }
        private void StartAutoScan() { /* Bắt đầu quét tự động */ }
        private void lvGameHistory_ColumnClick(object sender, ColumnClickEventArgs e) { /* Xử lý sắp xếp cột GameHistory */ }
        private void lvScanHistory_ColumnClick(object sender, ColumnClickEventArgs e) { /* Xử lý sắp xếp cột ScanHistory */ }
    }
}