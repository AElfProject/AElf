using AElf.Common;

namespace AElf.OS.Network.Events
{
    public class AnnoucementReceivedEventData
    {
        public Hash BlockId { get; private set; }
        public AnnoucementReceivedEventData(Hash blockId)
        {
            BlockId = blockId;
        }
    }
}