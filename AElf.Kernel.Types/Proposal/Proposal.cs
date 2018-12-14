using System.IO;
using AElf.Common;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

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
                new DoubleValue {Value = ExpiredTime}.WriteTo(stream);
                stream.Flush();
                mm.Flush();
                return Hash.FromRawBytes(mm.ToArray());
            }

        }
    }
}