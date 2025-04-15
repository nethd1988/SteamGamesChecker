using System;
using System.Globalization;
using System.Text.RegularExpressions;
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
            // Nếu đã có LastUpdateDateTime, sử dụng nó
            if (LastUpdateDateTime.HasValue)
            {
                // Cập nhật số ngày
                UpdateDaysCount = Math.Max(0, (int)(DateTime.Now - LastUpdateDateTime.Value).TotalDays);
                HasRecentUpdate = UpdateDaysCount < 7; // Mặc định là 7 ngày
                return;
            }

            if (string.IsNullOrEmpty(LastUpdate) || LastUpdate.Contains("Không"))
            {
                LastUpdateDateTime = null;
                return;
            }

            try
            {
                // Định dạng 1: Chuỗi có chứa GMT+7 hoặc định dạng Việt Nam
                if (LastUpdate.Contains("GMT+7") || LastUpdate.Contains("(GMT+7)"))
                {
                    // Đã ở định dạng Việt Nam, chỉ cần phân tích
                    string dateTimeStr = LastUpdate.Replace(" (GMT+7)", "");
                    if (DateTime.TryParseExact(dateTimeStr, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                    {
                        LastUpdateDateTime = parsedDate;
                        UpdateDaysCount = (int)(DateTime.Now - LastUpdateDateTime.Value).TotalDays;
                        HasRecentUpdate = UpdateDaysCount >= 0 && UpdateDaysCount < 7;
                        return;
                    }
                }

                // Định dạng 2: Timestamp Unix 10 hoặc 13 chữ số (giây hoặc mili giây)
                var timestampMatch = Regex.Match(LastUpdate, @"(\d{10,13})");
                if (timestampMatch.Success)
                {
                    string timestampStr = timestampMatch.Groups[1].Value;
                    long timestamp;
                    if (long.TryParse(timestampStr, out timestamp))
                    {
                        DateTime utcTime;
                        // Kiểm tra nếu là timestamp miligiây (13 chữ số)
                        if (timestampStr.Length >= 13)
                        {
                            utcTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(timestamp);
                        }
                        else
                        {
                            utcTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp);
                        }

                        // Chuyển sang giờ Việt Nam (UTC+7)
                        LastUpdateDateTime = utcTime.AddHours(7);
                        UpdateDaysCount = (int)(DateTime.Now - LastUpdateDateTime.Value).TotalDays;
                        HasRecentUpdate = UpdateDaysCount >= 0 && UpdateDaysCount < 7;
                        return;
                    }
                }

                // Định dạng 3: "dd MMMM yyyy - HH:mm:ss UTC"
                var utcMatch = Regex.Match(LastUpdate, @"(\d+)\s+([A-Za-z]+)\s+(\d{4})\s*[-–]\s*(\d{2}):(\d{2}):(\d{2})\s*UTC");
                if (utcMatch.Success)
                {
                    try
                    {
                        int day = int.Parse(utcMatch.Groups[1].Value);
                        string monthName = utcMatch.Groups[2].Value;
                        int year = int.Parse(utcMatch.Groups[3].Value);
                        int hour = int.Parse(utcMatch.Groups[4].Value);
                        int minute = int.Parse(utcMatch.Groups[5].Value);
                        int second = int.Parse(utcMatch.Groups[6].Value);

                        // Hỗ trợ nhiều ngôn ngữ cho tên tháng
                        int month;
                        // Thử phân tích với nhiều culture khác nhau
                        string[] cultures = { "en-US", "en-GB", "vi-VN" };
                        bool monthParsed = false;

                        foreach (string cultureName in cultures)
                        {
                            try
                            {
                                CultureInfo culture = new CultureInfo(cultureName);
                                for (int i = 1; i <= 12; i++)
                                {
                                    string monthFromCulture = culture.DateTimeFormat.GetMonthName(i);
                                    if (string.Compare(monthName, monthFromCulture, true, culture) == 0)
                                    {
                                        month = i;
                                        monthParsed = true;

                                        // Tạo DateTime UTC
                                        DateTime utcTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);

                                        // Chuyển sang giờ Việt Nam (UTC+7)
                                        LastUpdateDateTime = utcTime.AddHours(7);
                                        UpdateDaysCount = (int)(DateTime.Now - LastUpdateDateTime.Value).TotalDays;
                                        HasRecentUpdate = UpdateDaysCount >= 0 && UpdateDaysCount < 7;
                                        return;
                                    }
                                }
                            }
                            catch { }
                        }

                        // Nếu không phân tích được tên tháng, thử cách khác
                        if (!monthParsed)
                        {
                            // Danh sách tên tháng tiếng Anh
                            string[] englishMonths = {
                                "January", "February", "March", "April",
                                "May", "June", "July", "August",
                                "September", "October", "November", "December"
                            };

                            for (int i = 0; i < englishMonths.Length; i++)
                            {
                                if (string.Compare(monthName, englishMonths[i], true, CultureInfo.InvariantCulture) == 0)
                                {
                                    month = i + 1;

                                    // Tạo DateTime UTC
                                    DateTime utcTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);

                                    // Chuyển sang giờ Việt Nam (UTC+7)
                                    LastUpdateDateTime = utcTime.AddHours(7);
                                    UpdateDaysCount = (int)(DateTime.Now - LastUpdateDateTime.Value).TotalDays;
                                    HasRecentUpdate = UpdateDaysCount >= 0 && UpdateDaysCount < 7;
                                    return;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Lỗi khi xử lý chuỗi thời gian UTC: {ex.Message}");
                    }
                }

                // Định dạng 4: Thử với định dạng Việt Nam "dd/MM/yyyy HH:mm:ss"
                var vnMatch = Regex.Match(LastUpdate, @"(\d{1,2})/(\d{1,2})/(\d{4})\s+(\d{1,2}):(\d{1,2}):(\d{1,2})");
                if (vnMatch.Success)
                {
                    try
                    {
                        int day = int.Parse(vnMatch.Groups[1].Value);
                        int month = int.Parse(vnMatch.Groups[2].Value);
                        int year = int.Parse(vnMatch.Groups[3].Value);
                        int hour = int.Parse(vnMatch.Groups[4].Value);
                        int minute = int.Parse(vnMatch.Groups[5].Value);
                        int second = int.Parse(vnMatch.Groups[6].Value);

                        // Tạo DateTime cục bộ
                        LastUpdateDateTime = new DateTime(year, month, day, hour, minute, second);
                        UpdateDaysCount = (int)(DateTime.Now - LastUpdateDateTime.Value).TotalDays;
                        HasRecentUpdate = UpdateDaysCount >= 0 && UpdateDaysCount < 7;
                        return;
                    }
                    catch { }
                }

                // Định dạng 5: Định dạng chuỗi thời gian linh hoạt
                try
                {
                    // Thử phân tích chuỗi thời gian với nhiều culture khác nhau
                    string[] cultureNames = { "en-US", "en-GB", "vi-VN" };
                    foreach (string cultureName in cultureNames)
                    {
                        try
                        {
                            CultureInfo culture = new CultureInfo(cultureName);
                            if (DateTime.TryParse(LastUpdate, culture, DateTimeStyles.None, out DateTime parsedDate))
                            {
                                // Xác định đúng giờ cho thời gian
                                if (parsedDate.Kind == DateTimeKind.Utc)
                                {
                                    LastUpdateDateTime = parsedDate.AddHours(7);
                                }
                                else
                                {
                                    LastUpdateDateTime = parsedDate;
                                }

                                UpdateDaysCount = (int)(DateTime.Now - LastUpdateDateTime.Value).TotalDays;
                                HasRecentUpdate = UpdateDaysCount >= 0 && UpdateDaysCount < 7;
                                return;
                            }
                        }
                        catch { }
                    }
                }
                catch { }

                // Nếu còn các định dạng thời gian khác, thêm vào đây...

                // Không tìm thấy định dạng thời gian phù hợp
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
                // So sánh thời gian, cho phép sai số 1 giây
                TimeSpan diff = LastUpdateDateTime.Value - other.LastUpdateDateTime.Value;
                return diff.TotalSeconds > 1;
            }

            // Mặc định, kiểm tra nếu thông tin cập nhật khác nhau
            return LastUpdate != other.LastUpdate && !string.IsNullOrEmpty(LastUpdate) && !LastUpdate.Contains("Không");
        }

        /// <summary>
        /// Cập nhật thủ công thông tin thời gian cập nhật
        /// </summary>
        /// <param name="timestamp">Timestamp Unix</param>
        public void SetUpdateTimeFromTimestamp(long timestamp)
        {
            if (timestamp <= 0) return;

            try
            {
                DateTime utcTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp);

                // Chuyển sang GMT+7
                LastUpdateDateTime = utcTime.AddHours(7);
                LastUpdate = LastUpdateDateTime.Value.ToString("dd/MM/yyyy HH:mm:ss") + " (GMT+7)";
                UpdateDaysCount = (int)(DateTime.Now - LastUpdateDateTime.Value).TotalDays;
                HasRecentUpdate = UpdateDaysCount >= 0 && UpdateDaysCount < 7;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi cập nhật từ timestamp: {ex.Message}");
            }
        }

        /// <summary>
        /// Cập nhật thủ công thông tin thời gian cập nhật
        /// </summary>
        /// <param name="updateTime">Thời gian cập nhật</param>
        public void SetUpdateTime(DateTime updateTime)
        {
            try
            {
                LastUpdateDateTime = updateTime;
                LastUpdate = updateTime.ToString("dd/MM/yyyy HH:mm:ss") + " (GMT+7)";
                UpdateDaysCount = (int)(DateTime.Now - updateTime).TotalDays;
                HasRecentUpdate = UpdateDaysCount >= 0 && UpdateDaysCount < 7;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi cập nhật thời gian: {ex.Message}");
            }
        }
    }
}