using System.Collections.Generic;
using AElf.Types;

namespace AElf.Contracts.Profit.Models
{
    public class ClaimableProfit
    {
        public Hash SchemeId { get; set; }
        public long Period { get; set; }
        public long Shares { get; set; }
        public long TotalShares { get; set; }
        public Dictionary<string, long> AmountMap { get; set; }
    }
}