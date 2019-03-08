using System;
using System.IO;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Types.CSharp.Tests
{
    public class ConversionExtensionTests
    {
        [Fact]
        public void Deserialize_To_Bool()
        {
            var message = true.ToPbMessage();
            var bs = ByteString.FromStream(new MemoryStream(message.ToByteArray()));
            var boolValue = (bool)bs.DeserializeToType(typeof(bool));
            boolValue.ShouldBeTrue();
            
            var message1 = false.ToPbMessage();
            var bs1 = ByteString.FromStream(new MemoryStream(message1.ToByteArray()));
            var boolValue1 = (bool)bs1.DeserializeToType(typeof(bool));
            boolValue1.ShouldBeFalse();
        }
        
    }
}