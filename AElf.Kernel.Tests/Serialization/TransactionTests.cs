using System;
using AElf.Common.ByteArrayHelpers;
using AElf.Types.CSharp;
using Akka.Util;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Xunit;

namespace AElf.Kernel.Tests.Serialization
{
    public class TransactionTests
    {
        [Fact]
        public void FromTo()
        {
            Transaction t = new Transaction();
            t.From = new byte[] { 0x01, 0x02 };
            t.To = new byte[] { 0x03, 0x04 };

            byte[] b = t.ToByteArray();

            string bstr = b.ToHex();
            ;
            // bstr = CgQKAgECEgQKAgME
        }

        
        [Fact]
        public void Deserialize()
        {
            int a = 202;
            var aa = new SInt32Value
            {
                Value = 202
            };
            string ass = ByteString.CopyFrom(aa.ToByteArray()).ToByteArray().ToHex();
            System.Diagnostics.Debug.WriteLine(ass);
            var data = ByteArrayHelpers.FromHexString(ass);
            System.Diagnostics.Debug.WriteLine(SInt32Value.Parser.ParseFrom(data).Value);
            
            //System.Diagnostics.Debug.WriteLine(BytesValue.Parser.ParseFrom(data).ToHex());
            //System.Diagnostics.Debug.WriteLine(BoolValue.Parser.ParseFrom(data).Value);
            //System.Diagnostics.Debug.WriteLine(UInt64Value.Parser.ParseFrom(data.ToByteArray()).Value);

        }
    }
}