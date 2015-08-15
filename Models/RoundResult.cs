using System;
using System.ComponentModel.DataAnnotations;
using System.Data.Entity;
using System.Collections.Generic;

namespace CollapsedToto
{
    public class RoundResult
    {
        public RoundResult()
        {
        }
        [Key]
        public int RoundID { get; set; }
        public string Text { get; set; }
        virtual public List<KeyValuePair<string, int>> MatchedValues { get; set; }
        virtual public List<KeyValuePair<string, int>> UnmatchedValues { get; set; }
    }
}

