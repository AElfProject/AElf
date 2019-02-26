using System.Collections.Generic;
using AElf.Common;

namespace AElf.OS.Jobs
{
    public class ForkDownloadJobArgs
    {
        public string Peer { get; set; }
        public List<byte[]> BlockHashes { get; set; }
    }
}