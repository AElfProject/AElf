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
        }

        [Fact]
        public void Bytes_Equal()
        {
            var byteArray1 = Hash.Generate().DumpByteArray();
            var byteArray2 = Hash.Generate().DumpByteArray();
            var result = ByteArrayHelpers.BytesEqual(byteArray1, byteArray2);
            result.ShouldBe(false);

            var result1 = ByteArrayHelpers.BytesEqual(byteArray1, byteArray1);
            result1.ShouldBe(true);
        }

        [Fact]
        public void Bytes_Combine_And_SubArray()
        {
            var byteArray1 = Hash.Generate().DumpByteArray();
            var byteArray2 = Hash.Generate().DumpByteArray();
            var bytes = ByteArrayHelpers.Combine(byteArray1, byteArray2);
            bytes.Length.ShouldBe(byteArray1.Length + byteArray2.Length);

            var subArray1 = ByteArrayHelpers.SubArray(bytes, 0, byteArray1.Length);
            var subArray2 = ByteArrayHelpers.SubArray(bytes, byteArray1.Length, byteArray2.Length);
            subArray1.ShouldBe(byteArray1);
            subArray2.ShouldBe(byteArray2);
        }
    }
}