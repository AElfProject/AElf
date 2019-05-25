using AElf.Types;

namespace AElf.OS.Network.Infrastructure
{
    public class PeerBlockInfo
    {
        public Hash BlockHash { get; set; }
        
        public bool HasFork { get; set; }
    }
}