using System;
using System.IO;
using AElf.Common;
using AElf.Kernel;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Types.CSharp
{
    public class ConversionExtensionTests
    {
        [Fact]
        public void Deserialize_To_Bool()
        {
            //true value
            var stream = new MemoryStream(true.ToPbMessage().ToByteArray());
            var boolByteString = ByteString.FromStream(stream);
            var value1 = boolByteString.DeserializeToBool();
            value1.ShouldBe(true);

            //false value
            stream = new MemoryStream(false.ToPbMessage().ToByteArray());
            var boolByteString1 = ByteString.FromStream(stream);
            var value2 = boolByteString1.DeserializeToBool();
            value2.ShouldBe(false);
        }

        [Fact]
        public void Deserialize_To_Int()
        {
            var randomInt = new Random(DateTime.Now.Millisecond).Next(10_000, 80_000);
            var stream = new MemoryStream(randomInt.ToPbMessage().ToByteArray());
            var intByteString = ByteString.FromStream(stream);
            var serializedValue = intByteString.DeserializeToInt32();
            serializedValue.ShouldBe(randomInt);
        }

        [Fact]
        public void Deserialize_To_UInt()
        {
            var randomUInt = Convert.ToUInt32(new Random(DateTime.Now.Millisecond).Next());
            var stream = new MemoryStream(randomUInt.ToPbMessage().ToByteArray());
            var uintByteString = ByteString.FromStream(stream);
            var serializedValue = uintByteString.DeserializeToUInt32();
            serializedValue.ShouldBe(randomUInt);
        }

        [Fact]
        public void Deserialize_To_Long()
        {
            TransactionResult tr = new TransactionResult()
            {
                TransactionId = Hash.Generate(),

            };
        }

        [Fact]
        public void ByteString_Deserialize()
        {
            var bytes = new byte[]
            {
                125, 33, 27, 37, 202, 102, 171, 207, 118, 196, 214, 99, 224, 148, 157, 25, 230, 96, 125, 28, 227, 78, 1, 228,
                24, 161, 56, 125, 186, 214
            };
            var byteString = ByteString.CopyFrom(bytes);
            var newBytes = byteString.DeserializeToBytes();
            newBytes.ShouldBe(bytes);
        }
    }
}