using System.Collections.Generic;
using AElf.Common;

namespace AElf.OS.Jobs
{
    public class BlockSyncJobArgs
    {
        public long BlockHeight { get; set; }
        public Hash BlockHash { get; set; }
        
        /// <summary>
        /// leave it empty when you do not care get the block from which peer
        /// </summary>
        public string SuggestedPeerPubKey { get; set; }  
    }
}