using System;

namespace AElf.Management.Models
{
    public class NodeStateHistory
    {
        public DateTime Time { get; set; }

        public bool IsAlive { get; set; }

        public bool IsForked { get; set; }
    }
}