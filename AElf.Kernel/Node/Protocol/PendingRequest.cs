using System;
using System.Threading;

namespace AElf.Kernel.Node.Protocol
{
    public class PendingRequest
    {
        public int Id { get; set; }
        public AutoResetEvent ResetEvent { get; set; }
    }
}