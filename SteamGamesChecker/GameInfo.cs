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

        public GameInfo()
        {
            Name = "Không xác định";
            AppID = "";
            LastUpdate = "Không có thông tin";
            DaysAgo = "";
            Developer = "Không có thông tin";
            Publisher = "Không có thông tin";
            ReleaseDate = "Không có thông tin";
        }
    }
}