using System;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CollapsedToto
{
    public class User
    {
        // Dummy
        public User()
        {
        }

        public User(string userID)
        {
            UserID = userID;
            Point = 3141;
            PaneltyLevel = 0;
            UpgradeCount = 0;
        }

        [Key]
        [StringLength(20)]
        public string UserID { get; set; }
        // 소유포인트
        [Index]
        public long Point { get; set; }
        // 개인회생시에 요구되는 시간의 레벨
        public int PaneltyLevel { get; set; }
        [StringLength(64)]
        public string UserFullName { get; set; }
        [StringLength(32)]
        public string ScreenName { get; set; }
        [StringLength(128)]
        public string ProfileIconURL { get; set; }
        public DateTime ReviveRequestedTime { get; set; }
        public int UpgradeCount {  get; set; }
    }
}

