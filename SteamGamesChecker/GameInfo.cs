using System;
using System.Globalization;
using Newtonsoft.Json;

namespace SteamGamesChecker
{
    /// <summary>
    /// Lớp chứa thông tin về game
    /// </summary>
    public class GameInfo
    {
        public string Name { get; set; }
        public string AppID { get; set; }
        public string LastUpdate { get; set; }
        public string DaysAgo { get; set; }
        public string Developer { get; set; }
        public string Publisher { get; set; }
        public string ReleaseDate { get; set; }

        // Thêm thuộc tính mới
        public DateTime? LastUpdateDateTime { get; set; }
        public int UpdateDaysCount { get; set; }
        public bool HasRecentUpdate { get; set; }
        public string Status { get; set; }
        public long ChangeNumber { get; set; } // Số thay đổi từ SteamCMD API

        // Thêm thuộc tính cho file cập nhật lịch sử
        [JsonIgnore] // Không serialize thuộc tính này khi lưu vào file
        public DateTime LastChecked { get; set; }

        public GameInfo()
        {
            Name = "Không xác định";
            AppID = "";
            LastUpdate = "Không có thông tin";
            DaysAgo = "";
            Developer = "Không có thông tin";
            Publisher = "Không có thông tin";
            ReleaseDate = "Không có thông tin";
            Status = "Chưa xác định";
            HasRecentUpdate = false;
            UpdateDaysCount = -1;
            ChangeNumber = 0;
            LastChecked = DateTime.Now;
        }

        /// <summary>
        /// Cập nhật thuộc tính LastUpdateDateTime từ chuỗi LastUpdate hoặc timestamp
        /// </summary>
        public void UpdateLastUpdateDateTime()
        {
            if (string.IsNullOrEmpty(LastUpdate) || LastUpdate.Contains("Không"))
            {
                LastUpdateDateTime = null;
                return;
            }

            try
            {
                // Thử trích xuất chuỗi thời gian trong định dạng "dd MMMM yyyy - HH:mm:ss UTC"
                string pattern = @"(\d+)\s+([A-Za-z]+)\s+(\d{4})\s*[-–]\s*(\d{2}):(\d{2}):(\d{2})\s*UTC";
                System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(LastUpdate, pattern);

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

                    // Chuyển sang giờ Việt Nam (UTC+7)
                    LastUpdateDateTime = utcTime.AddHours(7);

                    // Cập nhật số ngày kể từ lần cập nhật cuối
                    UpdateDaysCount = (int)(DateTime.Now - LastUpdateDateTime.Value).TotalDays;

                    // Đánh dấu cập nhật gần đây nếu ít hơn số ngày cấu hình
                    HasRecentUpdate = UpdateDaysCount >= 0 && UpdateDaysCount < 7; // Mặc định là 7 ngày

                    return;
                }

                // Thử kiểm tra timestamp Unix
                pattern = @"(\d{10})";
                match = System.Text.RegularExpressions.Regex.Match(LastUpdate, pattern);
                if (match.Success)
                {
                    long timestamp = long.Parse(match.Groups[1].Value);
                    DateTime utcTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp);

                    // Chuyển sang giờ Việt Nam (UTC+7)
                    LastUpdateDateTime = utcTime.AddHours(7);

                    // Cập nhật số ngày kể từ lần cập nhật cuối
                    UpdateDaysCount = (int)(DateTime.Now - LastUpdateDateTime.Value).TotalDays;

                    // Đánh dấu cập nhật gần đây nếu ít hơn số ngày cấu hình
                    HasRecentUpdate = UpdateDaysCount >= 0 && UpdateDaysCount < 7; // Mặc định là 7 ngày

                    return;
                }

                // Thử phân tích chuỗi thời gian thông thường
                DateTime parsedDate;
                if (DateTime.TryParse(LastUpdate, out parsedDate))
                {
                    if (parsedDate.Kind == DateTimeKind.Utc)
                    {
                        // Nếu là UTC, chuyển sang giờ Việt Nam
                        LastUpdateDateTime = parsedDate.AddHours(7);
                    }
                    else if (parsedDate.Kind == DateTimeKind.Unspecified)
                    {
                        // Gán Kind là Local nếu không xác định
                        LastUpdateDateTime = DateTime.SpecifyKind(parsedDate, DateTimeKind.Local);
                    }
                    else
                    {
                        LastUpdateDateTime = parsedDate;
                    }

                    // Cập nhật số ngày kể từ lần cập nhật cuối
                    UpdateDaysCount = (int)(DateTime.Now - LastUpdateDateTime.Value).TotalDays;

                    // Đánh dấu cập nhật gần đây nếu ít hơn số ngày cấu hình
                    HasRecentUpdate = UpdateDaysCount >= 0 && UpdateDaysCount < 7; // Mặc định là 7 ngày

                    return;
                }

                LastUpdateDateTime = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi xử lý thời gian cập nhật: {ex.Message}");
                LastUpdateDateTime = null;
            }
        }

        /// <summary>
        /// Trả về thời gian cập nhật định dạng giờ Việt Nam
        /// </summary>
        public string GetVietnameseTimeFormat()
        {
            if (LastUpdateDateTime.HasValue)
            {
                return LastUpdateDateTime.Value.ToString("dd/MM/yyyy HH:mm:ss") + " (GMT+7)";
            }
            return LastUpdate;
        }

        /// <summary>
        /// So sánh với thông tin game khác để xem có cập nhật mới không
        /// </summary>
        /// <param name="other">Thông tin game trước đó</param>
        /// <returns>True nếu có cập nhật mới</returns>
        public bool HasNewerUpdate(GameInfo other)
        {
            if (other == null)
                return HasRecentUpdate;

            // So sánh theo ChangeNumber (nếu có)
            if (ChangeNumber > 0 && other.ChangeNumber > 0)
            {
                return ChangeNumber > other.ChangeNumber;
            }

            // So sánh theo thời gian cập nhật
            if (LastUpdateDateTime.HasValue && other.LastUpdateDateTime.HasValue)
            {
                return LastUpdateDateTime.Value > other.LastUpdateDateTime.Value;
            }

            // Mặc định, kiểm tra nếu thông tin cập nhật khác nhau
            return LastUpdate != other.LastUpdate && !string.IsNullOrEmpty(LastUpdate) && !LastUpdate.Contains("Không");
        }
    }
}