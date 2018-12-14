using System.IO;
using AElf.Common;
using Google.Protobuf;

namespace AElf.Kernel
{
    public partial class SideChainInfo
    {
        public Hash GetHash()
        {
            return Hash.FromRawBytes(this.ToByteArray());
        }
    }
}