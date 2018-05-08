using System.Collections.Generic;
using AElf.Kernel.Storages;

namespace AElf.Kernel
{
    public class TxResult
    {
        public Hash TxHash { get; set; }
        public Result Status { get; set; }
        public List<Change> Changes { get; set; }
    }

    public enum Result : int
    {
        Success = 1,
        Failed = 2
    }
}