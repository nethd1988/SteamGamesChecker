using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SteamGamesChecker
{
    /// <summary>
    /// Thông tin về client từ xa
    /// </summary>
    public class RemoteClient
    {
        public string Name { get; set; } = "";
        public string ApiUrl { get; set; } = "";
        public string ApiKey { get; set; } = "";
    }

    /// <summary>
    /// Dịch vụ gửi lệnh cập nhật đến client từ xa
    /// </summary>
    public class RemoteUpdateService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public string ClientApiUrl { get; set; }

        public RemoteUpdateService(string clientApiUrl, string apiKey)
        {
            ClientApiUrl = clientApiUrl.TrimEnd('/');
            _apiKey = apiKey;

            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("X-API-KEY", _apiKey);
            _httpClient.Timeout = TimeSpan.FromSeconds(30); // 30 giây timeout
        }

        /// <summary>
        /// Gửi lệnh cập nhật game đến client
        /// </summary>
        public async Task<bool> SendUpdateCommandAsync(string appId, string reason = "Phát hiện cập nhật mới")
        {
            try
            {
                // Tạo dữ liệu yêu cầu
                var requestData = new
                {
                    AppId = appId,
                    Reason = reason
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(requestData),
                    Encoding.UTF8,
                    "application/json");

                // Gửi yêu cầu POST
                var response = await _httpClient.PostAsync($"{ClientApiUrl}/api/Remote/run", content);

                // Kiểm tra phản hồi
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Gửi lệnh cập nhật thành công cho AppId {appId}: {responseContent}");
                    return true;
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    System.Diagnostics.Debug.WriteLine($"Lỗi khi gửi lệnh cập nhật cho AppId {appId}: {response.StatusCode}, {errorContent}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ngoại lệ khi gửi lệnh cập nhật cho AppId {appId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Kiểm tra kết nối với client
        /// </summary>
        public async Task<(bool Success, string Message)> TestConnectionAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync($"{ClientApiUrl}/api/Remote/ping");

                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();

                    // Parse JSON response
                    using (JsonDocument doc = JsonDocument.Parse(responseContent))
                    {
                        JsonElement root = doc.RootElement;
                        if (root.TryGetProperty("success", out JsonElement successElement) &&
                            successElement.GetBoolean())
                        {
                            string message = root.GetProperty("message").GetString();
                            string timestamp = root.TryGetProperty("timestamp", out JsonElement timestampElement)
                                ? timestampElement.GetString()
                                : DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                            string version = root.TryGetProperty("version", out JsonElement versionElement)
                                ? versionElement.GetString()
                                : "N/A";

                            return (true, $"Kết nối thành công! Server: {version}, Time: {timestamp}");
                        }
                        else
                        {
                            string error = root.TryGetProperty("error", out JsonElement errorElement)
                                ? errorElement.GetString()
                                : "Lỗi không xác định";
                            return (false, $"Lỗi: {error}");
                        }
                    }
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    return (false, $"Lỗi kết nối: {response.StatusCode}, {errorContent}");
                }
            }
            catch (HttpRequestException ex)
            {
                return (false, $"Không thể kết nối đến client: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi: {ex.Message}");
            }
        }
    }
}