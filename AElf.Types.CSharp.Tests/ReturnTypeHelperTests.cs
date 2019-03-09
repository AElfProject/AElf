using System;
using System.IO;
using AElf.Common;
using AElf.Kernel;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Types.CSharp
{
    public class ReturnTypeHelperTest
    {
        [Fact]
        public void Deserialize_To_Bool()
        {
            //true value
            {
                var bytes = ReturnTypeHelper.GetEncoder<bool>()(true);
                var decoded = ReturnTypeHelper.GetDecoder<bool>()(bytes);
                decoded.ShouldBe(true);
            }

            //false value
            {
                var bytes = ReturnTypeHelper.GetEncoder<bool>()(false);
                var decoded = ReturnTypeHelper.GetDecoder<bool>()(bytes);
                decoded.ShouldBe(false);      
            }

        }

        [Fact]
        public void Deserialize_To_Int()
        {
            var randomInt = new Random(DateTime.Now.Millisecond).Next(10_000, 80_000);
            var bytes = ReturnTypeHelper.GetEncoder<int>()(randomInt);
            var decoded = ReturnTypeHelper.GetDecoder<int>()(bytes);
            decoded.ShouldBe(randomInt);
        }

        [Fact]
        public void Deserialize_To_UInt()
        {
            var randomUInt = Convert.ToUInt32(new Random(DateTime.Now.Millisecond).Next());
            var bytes = ReturnTypeHelper.GetEncoder<uint>()(randomUInt);
            var decoded = ReturnTypeHelper.GetDecoder<uint>()(bytes);
            decoded.ShouldBe(randomUInt);
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
            var encoded = ReturnTypeHelper.GetEncoder<byte[]>()(bytes);
            var decoded = ReturnTypeHelper.GetEncoder<byte[]>()(encoded);
            decoded.ShouldBe(bytes);
        }
    }
}