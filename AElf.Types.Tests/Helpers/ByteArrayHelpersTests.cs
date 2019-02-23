using System.Collections.Generic;
using Xunit;
using AElf.Common;
using Shouldly;

namespace AElf.Types.Tests.Helpers
{
    public class ByteArrayHelpersTests
    {
        [Fact]
        public void Convert_Byte_FromString()
        {
            var hexValue = Hash.Generate().ToHex();
            var hashArray = ByteArrayHelpers.FromHexString(hexValue);
            hashArray.Length.ShouldBe(32);
            
            var value = "0x00";
            var valueArrary = ByteArrayHelpers.FromHexString(value);
            valueArrary.Length.ShouldBe(1);
        }

        [Fact]
        public void Bytes_Equal()
        {
            var byteArray1 = ByteArrayHelpers.RandomFill(10);
            var byteArray2 = ByteArrayHelpers.RandomFill(10);
            var byteArray3 = ByteArrayHelpers.RandomFill(11);
            var result = ByteArrayHelpers.BytesEqual(byteArray1, byteArray2);
            result.ShouldBe(false);

            var result1 = ByteArrayHelpers.BytesEqual(byteArray1, byteArray1);
            result1.ShouldBe(true);
            
            var result2 = ByteArrayHelpers.BytesEqual(byteArray1, byteArray3);
            result2.ShouldBe(false);
        }

        [Fact]
        public void Bytes_Combine_And_SubArray()
        {
            var byteArray1 = Hash.Generate().DumpByteArray();
            var byteArray2 = Hash.Generate().DumpByteArray();
            var bytes = ByteArrayHelpers.Combine(byteArray1, byteArray2);
            bytes.Length.ShouldBe(byteArray1.Length + byteArray2.Length);
            
            var bytes1 = ByteArrayHelpers.ConcatArrays(byteArray1,byteArray2,bytes);
            bytes1.Length.ShouldBe(byteArray1.Length + byteArray2.Length+bytes.Length);
            
            var subArray1 = ByteArrayHelpers.SubArray(bytes, 0, byteArray1.Length);
            var subArray2 = ByteArrayHelpers.SubArray(bytes, byteArray1.Length, byteArray2.Length);
            subArray1.ShouldBe(byteArray1);
            subArray2.ShouldBe(byteArray2);
        }
    }
}