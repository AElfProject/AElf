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
            var pcb = new ParentChainBlockInfo
            {
                Root = new ParentChainBlockRootInfo
                {
                    ChainId = Hash.Generate(),
                    Height = 1,
                    SideChainBlockHeadersRoot = Hash.Default,
                    SideChainTransactionsRoot = Hash.Default
                }
            };

            var bytes = ByteString.CopyFrom(ParamsPacker.Pack(pcb));
            
            
            /*var aa = new SInt32Value
            {
                Value = 202
            };
            string ass = ByteString.CopyFrom(aa.ToByteArray()).ToByteArray().ToHex();
            System.Diagnostics.Debug.WriteLine(ass);
            var data = ByteArrayHelpers.FromHexString(ass);*/
            System.Diagnostics.Debug.WriteLine((ParentChainBlockInfo)(ParamsPacker.Unpack(bytes.ToByteArray(), new[] {typeof(ParentChainBlockInfo)})[0]));
        }

        [Fact]
        public void DefaultValueTest()
        {
            var d = default(UInt64Value);
            System.Diagnostics.Debug.WriteLine(default(UInt64Value));
        }
        
    }
}