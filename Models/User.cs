using System;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations;

namespace CollapsedToto
{
    public class User
    {
        public User(string userID)
        {
            UserID = userID;
            Point = 3141;
            PaneltyLevel = 0;
        }

        [Key]
        [StringLength(20)]
        public string UserID { get; set; }
        // 소유포인트
        public long Point { get; set; }
        // 개인회생시에 요구되는 시간의 레벨
        public int PaneltyLevel { get; set; }
    }
}

