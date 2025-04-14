using System;
using System.Collections.Generic;

namespace SteamGamesChecker
{
    /// <summary>
    /// Lớp chứa thông tin lịch sử một lần quét
    /// </summary>
    public class ScanHistoryItem
    {
        /// <summary>
        /// Thời gian quét
        /// </summary>
        public DateTime ScanTime { get; set; }

        /// <summary>
        /// Tổng số game quét
        /// </summary>
        public int TotalGames { get; set; }

        /// <summary>
        /// Số game quét thành công
        /// </summary>
        public int SuccessCount { get; set; }

        /// <summary>
        /// Số game quét thất bại
        /// </summary>
        public int FailCount { get; set; }

        /// <summary>
        /// Danh sách game có cập nhật mới
        /// </summary>
        public List<string> UpdatedGames { get; set; }

        /// <summary>
        /// Phương thức API sử dụng
        /// </summary>
        public string ApiMethod { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public ScanHistoryItem()
        {
            ScanTime = DateTime.Now;
            TotalGames = 0;
            SuccessCount = 0;
            FailCount = 0;
            UpdatedGames = new List<string>();
            ApiMethod = "Unknown";
        }

        /// <summary>
        /// Lấy chuỗi hiển thị danh sách game cập nhật
        /// </summary>
        /// <returns>Chuỗi danh sách game</returns>
        public string GetUpdatedGamesString()
        {
            if (UpdatedGames == null || UpdatedGames.Count == 0)
                return "Không có";

            return string.Join(", ", UpdatedGames);
        }

        /// <summary>
        /// Lấy chuỗi thời gian quét định dạng Việt Nam
        /// </summary>
        /// <returns>Chuỗi thời gian</returns>
        public string GetScanTimeString()
        {
            return ScanTime.ToString("dd/MM/yyyy HH:mm:ss") + " (GMT+7)";
        }
    }

    /// <summary>
    /// Lớp quản lý lịch sử quét
    /// </summary>
    public class ScanHistoryManager
    {
        private const string HISTORY_FILE = "scan_history.json";
        private List<ScanHistoryItem> scanHistoryItems = new List<ScanHistoryItem>();
        private const int MAX_HISTORY_ITEMS = 100; // Giới hạn số lượng lịch sử lưu trữ

        /// <summary>
        /// Lấy singleton instance
        /// </summary>
        private static ScanHistoryManager instance;
        public static ScanHistoryManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new ScanHistoryManager();
                return instance;
            }
        }

        private ScanHistoryManager()
        {
            LoadScanHistory();
        }

        /// <summary>
        /// Thêm một lịch sử quét mới
        /// </summary>
        /// <param name="historyItem">Lịch sử quét cần thêm</param>
        public void AddScanHistory(ScanHistoryItem historyItem)
        {
            if (historyItem == null)
                return;

            // Thêm vào đầu danh sách
            scanHistoryItems.Insert(0, historyItem);

            // Giới hạn số lượng lịch sử
            if (scanHistoryItems.Count > MAX_HISTORY_ITEMS)
            {
                scanHistoryItems.RemoveRange(MAX_HISTORY_ITEMS, scanHistoryItems.Count - MAX_HISTORY_ITEMS);
            }

            // Lưu lịch sử
            SaveScanHistory();
        }

        /// <summary>
        /// Lấy tất cả lịch sử quét
        /// </summary>
        /// <returns>Danh sách lịch sử quét</returns>
        public List<ScanHistoryItem> GetAllScanHistory()
        {
            return new List<ScanHistoryItem>(scanHistoryItems);
        }

        /// <summary>
        /// Xóa tất cả lịch sử quét
        /// </summary>
        public void ClearScanHistory()
        {
            scanHistoryItems.Clear();
            SaveScanHistory();
        }

        /// <summary>
        /// Lưu lịch sử quét vào file
        /// </summary>
        private void SaveScanHistory()
        {
            try
            {
                string json = Newtonsoft.Json.JsonConvert.SerializeObject(scanHistoryItems, Newtonsoft.Json.Formatting.Indented);
                System.IO.File.WriteAllText(HISTORY_FILE, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi lưu lịch sử quét: {ex.Message}");
            }
        }

        /// <summary>
        /// Đọc lịch sử quét từ file
        /// </summary>
        private void LoadScanHistory()
        {
            try
            {
                if (System.IO.File.Exists(HISTORY_FILE))
                {
                    string json = System.IO.File.ReadAllText(HISTORY_FILE);
                    var history = Newtonsoft.Json.JsonConvert.DeserializeObject<List<ScanHistoryItem>>(json);
                    if (history != null)
                    {
                        scanHistoryItems = history;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi đọc lịch sử quét: {ex.Message}");
                scanHistoryItems = new List<ScanHistoryItem>();
            }
        }
    }
}