using AElf.Common;

namespace AElf.ChainController.EventMessages
{
    public class NewLibFound
    {
        public Hash BlockHash { get; set; }
        public ulong Height { get; set; }
    }
}