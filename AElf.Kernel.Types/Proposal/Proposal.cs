using System.IO;
using AElf.Common;
using Google.Protobuf;

namespace AElf.Kernel.Types.Proposal
{
    public partial class Proposal
    {
        public Hash GetHash()
        {
            using (var mm = new MemoryStream())
            using (var stream = new CodedOutputStream(mm))
            {
                MultiSigAccount.WriteTo(stream);
                Proposer.WriteTo(stream);
                TxnData.WriteTo(stream);
                ExpiredTime.WriteTo(stream);
                stream.Flush();
                mm.Flush();
                return Hash.FromRawBytes(mm.ToArray());
            }

        }
    }
}