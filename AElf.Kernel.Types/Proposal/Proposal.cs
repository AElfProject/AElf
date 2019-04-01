using AElf.Common;

namespace AElf.Kernel
{
    public partial class Proposal
    {
        public Hash GetHash()
        {
            return Hash.FromMessage(this);
        }
    }
}