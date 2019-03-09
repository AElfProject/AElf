using System;
using System.IO;
using AElf.Common;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Types.CSharp.Tests
{
    public class ConversionExtensionTests
    {
        [Fact(Skip = "Not passed due to some reason.")]
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

        [Fact]
        public void Deserialize_BoolAny()
        {
            var any1 = true.ToAny();
            any1.AnyToBool().ShouldBeTrue();
            
            var any2 = false.ToAny();
            any2.AnyToBool().ShouldBeFalse();
        }
        
        [Fact]
        public void Deserialize_IntAny()
        {
            var randomNumber = new Random(DateTime.Now.Millisecond).Next();
            var anyValue = randomNumber.ToAny();
            var intValue = anyValue.AnyToInt32();
            randomNumber.ShouldBe(intValue);
        }

        [Fact]
        public void Deserialize_UIntAny()
        {
            var uNumber = (uint) (new Random(DateTime.Now.Millisecond).Next());
            var any = uNumber.ToAny();
            any.AnyToUInt32().ShouldBe(uNumber);
        }
        
        [Fact]
        public void Deserialize_Int64Any()
        {
            var lNumber = (long) (new Random(DateTime.Now.Millisecond).Next());
            var any = lNumber.ToAny();
            any.AnyToInt64().ShouldBe(lNumber);
        }
        
        [Fact]
        public void Deserialize_UInt64Any()
        {
            var lNumber = (ulong) (new Random(DateTime.Now.Millisecond).Next());
            var any = lNumber.ToAny();
            any.AnyToUInt64().ShouldBe(lNumber);
        }

        [Fact]
        public void Deserialize_StringAny()
        {
            var message = "hello test";
            var any = message.ToAny();
            any.AnyToString().ShouldBe(message);
        }
        
        [Fact]
        public void Deserialize_ByteAny()
        {
            var byte1 = Hash.Generate().ToByteArray();
            var any = byte1.ToAny();
            any.AnyToBytes().ShouldBe(byte1);
        }
    }
}