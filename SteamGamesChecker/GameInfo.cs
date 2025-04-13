using System;

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
        }

        /// <summary>
        /// Cập nhật thuộc tính LastUpdateDateTime từ chuỗi LastUpdate
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
                    DateTime tempDate = DateTime.ParseExact(monthName, "MMMM", System.Globalization.CultureInfo.InvariantCulture);
                    int month = tempDate.Month;

                    // Tạo DateTime UTC
                    LastUpdateDateTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);

                    // Cập nhật số ngày kể từ lần cập nhật cuối
                    UpdateDaysCount = (int)(DateTime.UtcNow - LastUpdateDateTime.Value).TotalDays;

                    // Đánh dấu cập nhật gần đây nếu ít hơn 7 ngày
                    HasRecentUpdate = UpdateDaysCount < 7;

                    return;
                }

                // Thử kiểm tra timestamp Unix
                pattern = @"(\d{10})";
                match = System.Text.RegularExpressions.Regex.Match(LastUpdate, pattern);
                if (match.Success)
                {
                    long timestamp = long.Parse(match.Groups[1].Value);
                    LastUpdateDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp);

                    // Cập nhật số ngày kể từ lần cập nhật cuối
                    UpdateDaysCount = (int)(DateTime.UtcNow - LastUpdateDateTime.Value).TotalDays;

                    // Đánh dấu cập nhật gần đây nếu ít hơn 7 ngày
                    HasRecentUpdate = UpdateDaysCount < 7;

                    return;
                }

                LastUpdateDateTime = null;
            }
            catch
            {
                LastUpdateDateTime = null;
            }
        }
    }
}