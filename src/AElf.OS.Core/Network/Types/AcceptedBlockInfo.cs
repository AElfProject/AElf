using AElf.Types;

namespace AElf.OS.Network.Types
{
    public class AcceptedBlockInfo
    {
        public Hash BlockHash { get; set; }
        
        public bool HasFork { get; set; }
    }
}