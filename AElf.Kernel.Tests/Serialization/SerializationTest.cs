using AElf.Common;
using AElf.Types.CSharp;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace AElf.Kernel.Tests.Serialization
{
    public class SerializationTest
    {
        [Fact]
        public void FromTo()
        {
            Transaction t = new Transaction();
//            t.From = Address.FromBytes(new byte[] { 0x01, 0x02 });
//            t.To = Address.FromBytes(new byte[] { 0x03, 0x04 });

            byte[] b = t.ToByteArray();

            string bstr = b.ToHex();
            ;
            // bstr = CgQKAgECEgQKAgME
        }

        
        [Fact]
        public void Deserialize()
        {
            var bytes = ByteArrayHelpers.FromHexString(
                "0a200a1e9dee15619106b96861d52f03ad30ac7e57aa529eb2f05f7796472d8ce4a112200a1e96d8bf2dccf2ad419d02ed4a7b7a9d77df10617c4d731e766ce8dde63535320a496e697469616c697a653a0a0a015b120122180020005003");
            var txBytes = ByteString.CopyFrom(bytes).ToByteArray();
            var txn = Transaction.Parser.ParseFrom(txBytes);
            string str =txn.From.Value.ToByteArray().ToHex();
        }

        [Fact]
        public void DefaultValueTest()
        {
            var d = default(UInt64Value);
            System.Diagnostics.Debug.WriteLine(default(UInt64Value));
        }
        
    }
}