using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using Telegram.Bot.Types;
using System.Xml;

namespace SteamGamesChecker
{
    /// <summary>
    /// Lớp quản lý thông báo qua Telegram Bot
    /// </summary>
    public class TelegramNotifier
    {
        private ITelegramBotClient botClient;
        private string botToken;
        private List<long> chatIds = new List<long>();
        private string configPath = "telegram_config.json";
        private bool isEnabled = false;
        private int notificationThreshold = 7; // Mặc định thông báo cho game cập nhật trong 7 ngày

        // Singleton pattern
        private static TelegramNotifier instance;
        public static TelegramNotifier Instance
        {
            get
            {
                if (instance == null)
                    instance = new TelegramNotifier();
                return instance;
            }
        }

        private TelegramNotifier()
        {
            LoadConfig();
        }

        /// <summary>
        /// Cấu trúc lưu cấu hình Telegram
        /// </summary>
        private class TelegramConfig
        {
            public string BotToken { get; set; }
            public List<long> ChatIds { get; set; }
            public bool IsEnabled { get; set; }
            public int NotificationThreshold { get; set; }
        }

        /// <summary>
        /// Lưu cấu hình Telegram vào file
        /// </summary>
        public void SaveConfig()
        {
            try
            {
                TelegramConfig config = new TelegramConfig
                {
                    BotToken = botToken,
                    ChatIds = chatIds,
                    IsEnabled = isEnabled,
                    NotificationThreshold = notificationThreshold
                };

                string json = JsonConvert.SerializeObject(config, Formatting.Indented);
                File.WriteAllText(configPath, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi lưu cấu hình Telegram: {ex.Message}");
            }
        }

        /// <summary>
        /// Đọc cấu hình Telegram từ file
        /// </summary>
        public void LoadConfig()
        {
            try
            {
                if (File.Exists(configPath))
                {
                    string json = File.ReadAllText(configPath, Encoding.UTF8);
                    TelegramConfig config = JsonConvert.DeserializeObject<TelegramConfig>(json);

                    if (config != null)
                    {
                        botToken = config.BotToken;
                        chatIds = config.ChatIds ?? new List<long>();
                        isEnabled = config.IsEnabled;
                        notificationThreshold = config.NotificationThreshold;

                        // Khởi tạo bot client nếu đã có token
                        if (!string.IsNullOrEmpty(botToken))
                        {
                            InitializeBot();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi đọc cấu hình Telegram: {ex.Message}");
            }
        }

        /// <summary>
        /// Bật/tắt thông báo Telegram
        /// </summary>
        public bool IsEnabled
        {
            get => isEnabled;
            set
            {
                isEnabled = value;
                SaveConfig();
            }
        }

        /// <summary>
        /// Ngưỡng số ngày để thông báo cập nhật (mặc định là 7 ngày)
        /// </summary>
        public int NotificationThreshold
        {
            get => notificationThreshold;
            set
            {
                notificationThreshold = value;
                SaveConfig();
            }
        }

        /// <summary>
        /// Khởi tạo Bot Telegram
        /// </summary>
        /// <param name="token">Token của bot</param>
        /// <returns>Kết quả khởi tạo</returns>
        public bool InitializeBot(string token = null)
        {
            try
            {
                // Nếu có token mới thì cập nhật
                if (!string.IsNullOrEmpty(token))
                {
                    botToken = token;
                }

                // Nếu không có token, báo lỗi
                if (string.IsNullOrEmpty(botToken))
                {
                    return false;
                }

                // Tạo bot client
                botClient = new TelegramBotClient(botToken);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khởi tạo Telegram Bot: {ex.Message}");
                MessageBox.Show($"Lỗi khởi tạo Telegram Bot: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Thêm chat ID vào danh sách
        /// </summary>
        /// <param name="chatId">ID của chat cần thêm</param>
        public void AddChatId(long chatId)
        {
            if (!chatIds.Contains(chatId))
            {
                chatIds.Add(chatId);
                SaveConfig();
            }
        }

        /// <summary>
        /// Xóa chat ID khỏi danh sách
        /// </summary>
        /// <param name="chatId">ID của chat cần xóa</param>
        public void RemoveChatId(long chatId)
        {
            if (chatIds.Contains(chatId))
            {
                chatIds.Remove(chatId);
                SaveConfig();
            }
        }

        /// <summary>
        /// Lấy danh sách chat ID
        /// </summary>
        /// <returns>Danh sách chat ID</returns>
        public List<long> GetChatIds()
        {
            return new List<long>(chatIds);
        }

        /// <summary>
        /// Lấy token bot hiện tại
        /// </summary>
        /// <returns>Token bot hiện tại</returns>
        public string GetBotToken()
        {
            return botToken ?? string.Empty;
        }

        /// <summary>
        /// Thiết lập token bot
        /// </summary>
        /// <param name="token">Token của bot</param>
        /// <returns>Kết quả thiết lập</returns>
        public bool SetBotToken(string token)
        {
            if (string.IsNullOrEmpty(token))
                return false;

            botToken = token;
            SaveConfig();
            return InitializeBot();
        }

        /// <summary>
        /// Gửi tin nhắn thông báo về cập nhật game
        /// </summary>
        /// <param name="gameInfo">Thông tin game cần thông báo</param>
        /// <returns>Task kết quả gửi tin nhắn</returns>
        public async Task<bool> SendGameUpdateNotification(GameInfo gameInfo)
        {
            if (!isEnabled || botClient == null || chatIds.Count == 0 || gameInfo == null)
                return false;

            // Chỉ thông báo nếu game có cập nhật mới hơn ngưỡng cài đặt
            if (gameInfo.UpdateDaysCount > notificationThreshold)
                return false;

            try
            {
                // Tạo tin nhắn thông báo
                StringBuilder message = new StringBuilder();
                message.AppendLine("🎮 *Game có cập nhật mới* 🎮");
                message.AppendLine($"Tên: *{gameInfo.Name}*");
                message.AppendLine($"ID: `{gameInfo.AppID}`");
                message.AppendLine($"Cập nhật: {gameInfo.LastUpdate}");
                message.AppendLine($"({gameInfo.UpdateDaysCount} ngày trước)");

                if (!string.IsNullOrEmpty(gameInfo.Developer) && gameInfo.Developer != "Không có thông tin")
                    message.AppendLine($"Nhà phát triển: {gameInfo.Developer}");

                if (!string.IsNullOrEmpty(gameInfo.Publisher) && gameInfo.Publisher != "Không có thông tin")
                    message.AppendLine($"Nhà phát hành: {gameInfo.Publisher}");

                message.AppendLine($"Link: https://store.steampowered.com/app/{gameInfo.AppID}/");

                // Gửi tin nhắn đến tất cả các chat đã đăng ký
                foreach (long chatId in chatIds)
                {
                    try
                    {
                        await botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: message.ToString(),
                            parseMode: Telegram.Bot.Types.Enums.ParseMode.Markdown);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Lỗi gửi tin nhắn đến chat {chatId}: {ex.Message}");
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi gửi thông báo Telegram: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gửi tin nhắn kiểm tra
        /// </summary>
        /// <param name="chatId">ID của chat cần gửi</param>
        /// <returns>Kết quả gửi tin nhắn</returns>
        public async Task<bool> SendTestMessage(long chatId)
        {
            if (botClient == null)
                return false;

            try
            {
                await botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "✅ Đây là tin nhắn kiểm tra từ Steam Games Checker. Bot đã hoạt động thành công!");
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi gửi tin nhắn kiểm tra: {ex.Message}");
                MessageBox.Show($"Lỗi gửi tin nhắn: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// Chuyển đổi thời gian sang định dạng Việt Nam
        /// </summary>
        /// <param name="timeString">Chuỗi thời gian</param>
        /// <returns>Chuỗi thời gian định dạng Việt Nam</returns>
        public string ConvertToVietnamTime(string timeString)
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
    }
}