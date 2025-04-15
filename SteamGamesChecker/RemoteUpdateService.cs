using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SteamGamesChecker
{
    public class RemoteUpdateService
    {
        private readonly HttpClient _httpClient;
        private readonly string _clientApiUrl;
        private readonly string _apiKey;
        
        public RemoteUpdateService(string clientApiUrl, string apiKey)
        {
            _httpClient = new HttpClient();
            _clientApiUrl = clientApiUrl.TrimEnd('/') + "/api/Remote/run";
            _apiKey = apiKey;

            // Thêm API key vào header
            _httpClient.DefaultRequestHeaders.Add("X-API-KEY", _apiKey);
        }

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
                var response = await _httpClient.PostAsync(_clientApiUrl, content);
                
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
    }
}