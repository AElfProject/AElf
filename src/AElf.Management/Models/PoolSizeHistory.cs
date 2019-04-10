using System;

namespace AElf.Management.Models
{
    public class PoolSizeHistory
    {
        public DateTime Time { get; set; }

        public ulong Size { get; set; }
    }
}