using System;

namespace AElf.Management.Models
{
    public class PoolStateHistory
    {
        public DateTime Time { get; set; }

        public int RequestPoolSize { get; set; }

        public int ReceivePoolSize { get; set; }
    }
}