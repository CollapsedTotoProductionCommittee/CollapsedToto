using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace CollapsedToto
{
    public class UserRound
    {
        [Key]
        [ForeignKey("Owner")]
        [StringLength(20)]
        [Column(Order = 1)]
        public string OwnerID { get; set; }

        public virtual User Owner { get; set; }

        [Key]
        [Column(Order = 2)]
        public int RoundID { get; set; }

        virtual public Dictionary<string, int> BettedWords { get; set; }
    }
}

