using System.IO;
using Google.Protobuf;

namespace AElf.Kernel
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
                stream.WriteBytes(TxnData);
                ExpiredTime.WriteTo(stream);
                stream.Flush();
                mm.Flush();
                return Hash.FromRawBytes(mm.ToArray());
            }
        }
    }
}