using AElf.Types;

namespace AElf.OS.BlockSync.Events
{
    public class BlockValidationFailedEventData
    {
        public Hash BlockHash { get; set;}
        public long BlockHeight { get; set;}
        public string BlockSenderPubkey { get; set; }
    }
}