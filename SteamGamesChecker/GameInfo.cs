using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace SteamGamesChecker
{
    public class GameInfo
    {
        public string Name { get; set; }
        public string AppID { get; set; }
        public string LastUpdate { get; set; }
        public string DaysAgo { get; set; }
        public string Developer { get; set; }
        public string Publisher { get; set; }
        public string ReleaseDate { get; set; }
        public DateTime? LastUpdateDateTime { get; set; }
        public int UpdateDaysCount { get; set; }
        public bool HasRecentUpdate { get; set; }
        public string Status { get; set; }
        public long ChangeNumber { get; set; }
        [JsonIgnore]
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

        public void UpdateLastUpdateDateTime()
        {
            if (LastUpdateDateTime.HasValue)
            {
                // Kiểm tra thời gian hợp lệ và tính số ngày
                UpdateDaysCount = (int)(DateTime.Now - LastUpdateDateTime.Value).TotalDays;
                if (UpdateDaysCount < 0)
                {
                    // Thời gian trong tương lai, hiển thị thông tin phù hợp
                    UpdateDaysCount = 0; // Đặt về 0 để tránh hiển thị số âm
                    Status = "Thời gian cập nhật trong tương lai";
                }
                else
                {
                    Status = "Đã cập nhật";
                }
                HasRecentUpdate = UpdateDaysCount >= 0 && UpdateDaysCount < 7;
                return;
            }

            if (string.IsNullOrEmpty(LastUpdate) || LastUpdate.Contains("Không"))
            {
                LastUpdateDateTime = null;
                UpdateDaysCount = -1;
                HasRecentUpdate = false;
                Status = "Không có thông tin";
                return;
            }

            try
            {
                // Định dạng GMT+7 (đã được chuẩn hóa trong ứng dụng)
                if (LastUpdate.Contains("GMT+7") || LastUpdate.Contains("(GMT+7)"))
                {
                    string dateTimeStr = LastUpdate.Replace(" (GMT+7)", "");
                    if (DateTime.TryParseExact(dateTimeStr, "dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                    {
                        LastUpdateDateTime = parsedDate;
                        UpdateDaysCount = (int)(DateTime.Now - LastUpdateDateTime.Value).TotalDays;
                        HasRecentUpdate = UpdateDaysCount >= 0 && UpdateDaysCount < 7;
                        Status = UpdateDaysCount < 0 ? "Thời gian cập nhật trong tương lai" : "Đã cập nhật";
                        return;
                    }
                    System.Diagnostics.Debug.WriteLine($"Failed to parse GMT+7 format: {LastUpdate}");
                }

                // Định dạng timestamp (Unix)
                var timestampMatch = Regex.Match(LastUpdate, @"(\d{10,13})");
                if (timestampMatch.Success)
                {
                    string timestampStr = timestampMatch.Groups[1].Value;
                    if (long.TryParse(timestampStr, out long timestamp))
                    {
                        DateTime utcTime;
                        if (timestampStr.Length >= 13)
                        {
                            utcTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(timestamp);
                        }
                        else
                        {
                            utcTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp);
                        }
                        LastUpdateDateTime = utcTime.AddHours(7);
                        UpdateDaysCount = (int)(DateTime.Now - LastUpdateDateTime.Value).TotalDays;
                        HasRecentUpdate = UpdateDaysCount >= 0 && UpdateDaysCount < 7;
                        Status = UpdateDaysCount < 0 ? "Thời gian cập nhật trong tương lai" : "Đã cập nhật";
                        return;
                    }
                    System.Diagnostics.Debug.WriteLine($"Failed to parse timestamp: {LastUpdate}");
                }

                // Định dạng UTC từ SteamDB (e.g., "13 April 2025 – 22:55:30 UTC")
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

                        int month;
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
                                        DateTime utcTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
                                        LastUpdateDateTime = utcTime.AddHours(7);
                                        UpdateDaysCount = (int)(DateTime.Now - LastUpdateDateTime.Value).TotalDays;
                                        HasRecentUpdate = UpdateDaysCount >= 0 && UpdateDaysCount < 7;
                                        Status = UpdateDaysCount < 0 ? "Thời gian cập nhật trong tương lai" : "Đã cập nhật";
                                        return;
                                    }
                                }
                            }
                            catch { }
                        }

                        if (!monthParsed)
                        {
                            string[] englishMonths = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
                            for (int i = 0; i < englishMonths.Length; i++)
                            {
                                if (string.Compare(monthName, englishMonths[i], true, CultureInfo.InvariantCulture) == 0)
                                {
                                    month = i + 1;
                                    DateTime utcTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
                                    LastUpdateDateTime = utcTime.AddHours(7);
                                    UpdateDaysCount = (int)(DateTime.Now - LastUpdateDateTime.Value).TotalDays;
                                    HasRecentUpdate = UpdateDaysCount >= 0 && UpdateDaysCount < 7;
                                    Status = UpdateDaysCount < 0 ? "Thời gian cập nhật trong tương lai" : "Đã cập nhật";
                                    return;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Lỗi khi xử lý chuỗi thời gian UTC: {ex.Message}, LastUpdate: {LastUpdate}");
                    }
                }

                // Định dạng Việt Nam (e.g., "dd/MM/yyyy HH:mm:ss")
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
                        LastUpdateDateTime = new DateTime(year, month, day, hour, minute, second);
                        UpdateDaysCount = (int)(DateTime.Now - LastUpdateDateTime.Value).TotalDays;
                        HasRecentUpdate = UpdateDaysCount >= 0 && UpdateDaysCount < 7;
                        Status = UpdateDaysCount < 0 ? "Thời gian cập nhật trong tương lai" : "Đã cập nhật";
                        return;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Lỗi khi xử lý định dạng thời gian Việt Nam: {ex.Message}, LastUpdate: {LastUpdate}");
                    }
                }

                // Thử phân tích thời gian theo các định dạng khác
                try
                {
                    string[] cultureNames = { "en-US", "en-GB", "vi-VN" };
                    foreach (string cultureName in cultureNames)
                    {
                        try
                        {
                            CultureInfo culture = new CultureInfo(cultureName);
                            if (DateTime.TryParse(LastUpdate, culture, DateTimeStyles.None, out DateTime parsedDate))
                            {
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
                                Status = UpdateDaysCount < 0 ? "Thời gian cập nhật trong tương lai" : "Đã cập nhật";
                                return;
                            }
                        }
                        catch { }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Lỗi khi phân tích thời gian chung: {ex.Message}, LastUpdate: {LastUpdate}");
                }

                // Nếu không phân tích được, đặt lại giá trị mặc định
                LastUpdateDateTime = null;
                UpdateDaysCount = -1;
                HasRecentUpdate = false;
                Status = "Không có thông tin";
                System.Diagnostics.Debug.WriteLine($"Failed to parse LastUpdate: {LastUpdate}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi xử lý thời gian cập nhật: {ex.Message}, LastUpdate: {LastUpdate}");
                LastUpdateDateTime = null;
                UpdateDaysCount = -1;
                HasRecentUpdate = false;
                Status = "Không có thông tin";
            }
        }

        public string GetVietnameseTimeFormat()
        {
            if (LastUpdateDateTime.HasValue)
            {
                return LastUpdateDateTime.Value.ToString("dd/MM/yyyy HH:mm:ss") + " (GMT+7)";
            }
            return LastUpdate;
        }

        public bool HasNewerUpdate(GameInfo other)
        {
            if (other == null)
                return HasRecentUpdate;

            if (ChangeNumber > 0 && other.ChangeNumber > 0)
            {
                return ChangeNumber > other.ChangeNumber;
            }

            if (LastUpdateDateTime.HasValue && other.LastUpdateDateTime.HasValue)
            {
                TimeSpan diff = LastUpdateDateTime.Value - other.LastUpdateDateTime.Value;
                return diff.TotalSeconds > 1;
            }

            return LastUpdate != other.LastUpdate && !string.IsNullOrEmpty(LastUpdate) && !LastUpdate.Contains("Không");
        }

        public void SetUpdateTimeFromTimestamp(long timestamp)
        {
            if (timestamp <= 0) return;

            try
            {
                DateTime utcTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(timestamp);
                LastUpdateDateTime = utcTime.AddHours(7);
                LastUpdate = LastUpdateDateTime.Value.ToString("dd/MM/yyyy HH:mm:ss") + " (GMT+7)";
                UpdateDaysCount = (int)(DateTime.Now - LastUpdateDateTime.Value).TotalDays;
                HasRecentUpdate = UpdateDaysCount >= 0 && UpdateDaysCount < 7;
                Status = UpdateDaysCount < 0 ? "Thời gian cập nhật trong tương lai" : "Đã cập nhật";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi cập nhật từ timestamp: {ex.Message}, Timestamp: {timestamp}");
                LastUpdateDateTime = null;
                UpdateDaysCount = -1;
                HasRecentUpdate = false;
                Status = "Không có thông tin";
            }
        }

        public void SetUpdateTime(DateTime updateTime)
        {
            try
            {
                LastUpdateDateTime = updateTime;
                LastUpdate = updateTime.ToString("dd/MM/yyyy HH:mm:ss") + " (GMT+7)";
                UpdateDaysCount = (int)(DateTime.Now - updateTime).TotalDays;
                HasRecentUpdate = UpdateDaysCount >= 0 && UpdateDaysCount < 7;
                Status = UpdateDaysCount < 0 ? "Thời gian cập nhật trong tương lai" : "Đã cập nhật";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi cập nhật thời gian: {ex.Message}, UpdateTime: {updateTime}");
                LastUpdateDateTime = null;
                UpdateDaysCount = -1;
                HasRecentUpdate = false;
                Status = "Không có thông tin";
            }
        }
    }
}