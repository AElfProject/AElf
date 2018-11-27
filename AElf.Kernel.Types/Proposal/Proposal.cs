using AElf.Common;
using Google.Protobuf;

namespace AElf.Kernel.Types.Proposal
{
    public partial class Proposal
    {
        public Hash GetHash()
        {
            return Hash.FromRawBytes(this.ToByteArray());
        }
    }
}