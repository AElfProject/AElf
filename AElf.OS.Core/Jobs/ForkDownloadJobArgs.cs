using System.Collections.Generic;
using AElf.Common;

namespace AElf.OS.Jobs
{
    public class ForkDownloadJobArgs
    {
        public int ChainId { get; set; }
        public ulong BlockHeight { get; set; }
        public long BlockHash { get; set; }
        
        /// <summary>
        /// leave it empty when you do not care get the block from which peer
        /// </summary>
        public string SuggestedPeerAddress { get; set; }  
    }
}