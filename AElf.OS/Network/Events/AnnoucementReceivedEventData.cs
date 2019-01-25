using AElf.Common;

namespace AElf.OS.Network
{
    public class AnnoucementReceivedEventData
    {
        public Hash _blockId { get; private set; }
        public AnnoucementReceivedEventData(Hash blockId)
        {
            
        }
    }
}