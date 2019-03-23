using System;

namespace AElf.Management.Models
{
    public class BlockInfoResult
    {
        public BlockInfoHeader Header { get; set; }

        public BlockInfoBody Body { get; set; }
    }

    public class BlockInfoHeader
    {
        public DateTime Time { get; set; }
    }

    public class BlockInfoBody
    {
        public int TransactionsCount { get; set; }
    }
}