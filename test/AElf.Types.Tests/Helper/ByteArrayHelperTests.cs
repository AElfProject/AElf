using System;
using Xunit;
using Shouldly;

namespace AElf.Types.Tests.Helper
{
    public class ByteArrayHelperTests
    {
        [Fact]
        public void Convert_Byte_FromString()
        {
            var hexValue = Hash.FromString("hexvalue").ToHex();
            var hashArray = ByteArrayHelper.HexStringToByteArray(hexValue);
            hashArray.Length.ShouldBe(32);

            var value = "0x00";
            var valueArrary = ByteArrayHelper.HexStringToByteArray(value);
            valueArrary.Length.ShouldBe(1);
        }


        static Random _rnd = new Random();

        public static byte[] RandomFill(int count)
        {
            byte[] random = new byte[count];

            _rnd.NextBytes(random);

            return random;
        }

        [Fact]
        public void Bytes_Equal()
        {
            var byteArray1 = RandomFill(10);
            var byteArray2 = RandomFill(10);
            var byteArray3 = RandomFill(11);
            var result = ByteArrayHelper.BytesEqual(byteArray1, byteArray2);
            result.ShouldBe(false);

            var result1 = ByteArrayHelper.BytesEqual(byteArray1, byteArray1);
            result1.ShouldBe(true);

            var result2 = ByteArrayHelper.BytesEqual(byteArray1, byteArray3);
            result2.ShouldBe(false);
        }

        [Fact]
        public void LeftPadBytes_Test()
        {
            var bytes = new byte[] {1, 2, 3, 4};
            var expectBytes = new byte[] {0, 0, 0, 0, 1, 2, 3, 4};

            var result1 = bytes.LeftPad(8);
            var result2 = bytes.LeftPad(2);

            result1.ShouldBe(expectBytes);
            result2.ShouldBe(bytes);
        }
    }
}