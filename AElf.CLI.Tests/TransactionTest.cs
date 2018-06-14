using System;
using System.IO;
using AElf.CLI.Data.Protobuf;
using ProtoBuf;
using Xunit;

namespace AElf.CLI.Tests
{
    public class TransactionTest
    {
        [Fact]
        public void FromTo()
        {
            string expect = "CgQKAgECEgQKAgME";
            
            Transaction t = new Transaction();
            t.From = new byte[] { 0x01, 0x02 };
            t.To = new byte[] { 0x03, 0x04 };
            
            MemoryStream ms = new MemoryStream();
            Serializer.Serialize(ms, t);

            byte[] b = ms.ToArray();
            
            string bstr = Convert.ToBase64String(b);
            
            Assert.Equal(expect, bstr);
        }
    }
}