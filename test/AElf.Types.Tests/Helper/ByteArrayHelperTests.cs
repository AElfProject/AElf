using System;
using System.Linq;
using Xunit;
using Shouldly;

namespace AElf.Types.Tests.Helper
{
    public class ByteArrayHelperTests
    {
        [Fact]
        public void Convert_Byte_FromString_Test()
        {
            var hexValue = HashHelper.ComputeFrom("hexvalue").ToHex();
            var hashArray = ByteArrayHelper.HexStringToByteArray(hexValue);
            hashArray.Length.ShouldBe(32);

            var value = "0x00";
            var valueArrary = ByteArrayHelper.HexStringToByteArray(value);
            valueArrary.Length.ShouldBe(1);
        }


        static Random _rnd = new Random();

        private static byte[] RandomFill(int count)
        {
            byte[] random = new byte[count];

            _rnd.NextBytes(random);

            return random;
        }

        [Fact]
        public void Bytes_Equal_Test()
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
            
            byte[] byteArray4 = new byte[10];
            for (int i = 0; i < 10; i++)
            {
                byteArray4[i] = byteArray1[i];
            }
            var result3=   byteArray1.BytesEqual(byteArray4);
            result3.ShouldBe(true);
            
            byte[] byteArray5 = null;
            var result4 = byteArray5.BytesEqual(byteArray4);
            result4.ShouldBe(false);
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

        [Fact]
        public void ConcatArrays_Test()
        {
            var bytes1 = new byte[] {1, 2, 3, 4};
            var bytes2 = new byte[] {5, 6, 7, 8};
            var concatBytes = ByteArrayHelper.ConcatArrays(bytes1, bytes2);
            concatBytes.Length.ShouldBe(8);
            for(var i =0; i < bytes1.Length; i ++)
                concatBytes[i].ShouldBe(bytes1[i]);
            for(var i = bytes1.Length; i < concatBytes.Length; i ++)
                concatBytes[i].ShouldBe(bytes2[i - bytes1.Length]);
        }

        [Theory]
        [InlineData( new byte[]{1,2,3}, 1 , 1)]
        [InlineData( new byte[]{1,2,3}, 1 , 2)]
        [InlineData( new byte[]{1,2,3}, 1 , 0)]
        [InlineData( new byte[]{1,2,3}, 2 , 0)]
        public void SubArray_Test(byte[] array, int startIndex, int length)
        {
            if (length > 0)
            {
                var subArray = ByteArrayHelper.SubArray(array, startIndex, length);
                subArray.Length.ShouldBe(length);
                for (var i = 0; i < length; i++)
                {
                    subArray[i].ShouldBe(array[startIndex + i]);
                }
            }
            else
            {
                var subArray = ByteArrayHelper.SubArray(array, startIndex);
                subArray.Length.ShouldBe(array.Length - startIndex);
                for (var i = 0; i < length; i++)
                {
                    subArray[i].ShouldBe(array[startIndex + i]);
                }
            }
        }
    }
}