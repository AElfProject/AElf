using System.Collections.Generic;
using AElf.Kernel;

namespace AElf.Synchronization.EventMessages
{
    public class HeadersReceived
    {
        public List<BlockHeader> Headers { get; }

        public HeadersReceived(List<BlockHeader> headers)
        {
            Headers = headers;
        }
    }
}