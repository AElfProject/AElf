using System.Collections.Generic;
using AElf.Kernel;

namespace AElf.Node.EventMessages
{
    public class HeadersReceived
    {
        public List<BlockHeader> Headers { get; set; }

        public HeadersReceived(List<BlockHeader> headers)
        {
            Headers = headers;
        }
    }
}