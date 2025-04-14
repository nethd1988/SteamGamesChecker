using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace SteamGamesChecker
{
    /// <summary>
    /// Lớp quản lý lịch sử thông tin game
    /// </summary>
    public class GameHistoryManager
    {
        private const string HISTORY_FILE = "game_history.json";
        private const string LAST_SCAN_FILE = "last_scan.txt";
        private Dictionary<string, GameInfo> gameInfos = new Dictionary<string, GameInfo>();

        /// <summary>
        /// Lấy singleton instance
        /// </summary>
        private static GameHistoryManager instance;
        public static GameHistoryManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new GameHistoryManager();
                return instance;
            }
        }

        private GameHistoryManager()
        {
            LoadGameHistory();
        }

        /// <summary>
        /// Lấy thông tin game từ lịch sử
        /// </summary>
        /// <param name="appId">ID của game cần lấy</param>
        /// <returns>GameInfo hoặc null nếu không tìm thấy</returns>
        public GameInfo GetGameInfo(string appId)
        {
            if (gameInfos.ContainsKey(appId))
                return gameInfos[appId];
            return null;
        }

        /// <summary>
        /// Thêm hoặc cập nhật thông tin game trong lịch sử
        /// </summary>
        /// <param name="gameInfo">Thông tin game cần thêm/cập nhật</param>
        /// <returns>True nếu là cập nhật mới, False nếu không có thay đổi</returns>
        public bool AddOrUpdateGameInfo(GameInfo gameInfo)
        {
            bool isNewUpdate = false;
            if (gameInfo == null || string.IsNullOrEmpty(gameInfo.AppID))
                return false;

            // Cập nhật thời gian kiểm tra
            gameInfo.LastChecked = DateTime.Now;

            // Kiểm tra xem đã có thông tin game này chưa
            if (gameInfos.ContainsKey(gameInfo.AppID))
            {
                // So sánh thông tin mới và cũ
                GameInfo oldInfo = gameInfos[gameInfo.AppID];
                isNewUpdate = gameInfo.HasNewerUpdate(oldInfo);

                // Cập nhật thông tin mới
                gameInfos[gameInfo.AppID] = gameInfo;
            }
            else
            {
                // Thêm game mới
                gameInfos.Add(gameInfo.AppID, gameInfo);
                isNewUpdate = gameInfo.HasRecentUpdate;
            }

            // Lưu lịch sử
            SaveGameHistory();

            return isNewUpdate;
        }

        /// <summary>
        /// Lấy tất cả thông tin game trong lịch sử
        /// </summary>
        /// <returns>Danh sách thông tin game</returns>
        public List<GameInfo> GetAllGameInfos()
        {
            return gameInfos.Values.ToList();
        }

        /// <summary>
        /// Xóa thông tin game khỏi lịch sử
        /// </summary>
        /// <param name="appId">ID của game cần xóa</param>
        public bool RemoveGameInfo(string appId)
        {
            if (gameInfos.ContainsKey(appId))
            {
                gameInfos.Remove(appId);
                SaveGameHistory();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Lưu lịch sử quét
        /// </summary>
        public void SaveLastScanTime()
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                File.WriteAllText(LAST_SCAN_FILE, timestamp);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi lưu thời gian quét cuối: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy thời gian quét cuối
        /// </summary>
        /// <returns>Thời gian quét cuối hoặc null nếu chưa quét</returns>
        public DateTime? GetLastScanTime()
        {
            try
            {
                if (File.Exists(LAST_SCAN_FILE))
                {
                    string timestamp = File.ReadAllText(LAST_SCAN_FILE);
                    if (DateTime.TryParse(timestamp, out DateTime lastScan))
                        return lastScan;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi đọc thời gian quét cuối: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Lưu lịch sử thông tin game
        /// </summary>
        private void SaveGameHistory()
        {
            try
            {
                string json = JsonConvert.SerializeObject(gameInfos, Formatting.Indented);
                File.WriteAllText(HISTORY_FILE, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi lưu lịch sử game: {ex.Message}");
            }
        }

        /// <summary>
        /// Đọc lịch sử thông tin game
        /// </summary>
        private void LoadGameHistory()
        {
            try
            {
                if (File.Exists(HISTORY_FILE))
                {
                    string json = File.ReadAllText(HISTORY_FILE);
                    gameInfos = JsonConvert.DeserializeObject<Dictionary<string, GameInfo>>(json) ?? new Dictionary<string, GameInfo>();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Lỗi khi đọc lịch sử game: {ex.Message}");
                gameInfos = new Dictionary<string, GameInfo>();
            }
        }
    }
}