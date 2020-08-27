using System.Linq;
using System.Numerics;
using System.Text;
using AElf.Cryptography.SecretSharing;
using Shouldly;
using Xunit;

namespace AElf.Cryptography.Tests
{
    public class SecretSharingTest
    {
        [Theory]
        [InlineData(new byte[]{0x1, 0xff}, new byte[]{0xff})]
        [InlineData(new byte[]{0x1, 0x2, 0x3, 0x4}, new byte[]{0x4, 0x3, 0x2})]
        public void BigIntegerToBytesArrayTest(byte[] dataArray, byte[] expectedArray)
        {
            var number = new BigInteger(dataArray);
            var numberByteArray = number.ToBytesArray();
            numberByteArray.Length.ShouldBe(expectedArray.Length);
            for (var i = 0; i < expectedArray.Length; i++)
            {
                expectedArray[i].ShouldBe(numberByteArray[i]);
            }
        }
        
        [Theory]
        [InlineData(new byte[]{0xff}, new byte[]{0x00, 0xff})]
        [InlineData(new byte[]{0xff, 0x14, 0x54}, new byte[]{0x00, 0x54, 0x14, 0xff})]
        public void BytesArrayToBigIntegerTest(byte[] dataArray, byte[] expectedArray)
        {
            var byteArrayToNumber = dataArray.ToBigInteger();
            var newDataArray = byteArrayToNumber.ToByteArray();
            newDataArray.Length.ShouldBe(expectedArray.Length);
            for (var i = 0; i < expectedArray.Length; i++)
            {
                expectedArray[i].ShouldBe(newDataArray[i]);
            }
        }
        
        [Fact]
        public void BigIntegerAbsTest()
        {
            var dataArray = new byte[] {0xff, 0xff};
            var rawData = new BigInteger(dataArray);
            rawData.ShouldBe(-1);
            var absData = rawData.Abs();
            absData.ShouldBe(SecretSharingConsts.FieldPrime - 1);
        }
        
        [Fact]
        public void BigIntegerToStringTest()
        {
            var bigInteger =
                BigInteger.Parse("68117399500852794112165623985513434038399476516881142682654290358811497358689");
            var str = bigInteger.ConvertToString();
            Assert.Equal("aelf", str);
        }

        [Theory]
        [InlineData("gggaelf", 3, 9)]
        [InlineData("bbbaelf", 12, 17)]
        [InlineData("kkkkaelf", 5, 20)]
        public void SharingTest(string str, int threshold, int totalParts)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            var parts = SecretSharingHelper.EncodeSecret(bytes, threshold, totalParts);
            Assert.Equal(totalParts, parts.Count);

            var result = SecretSharingHelper.DecodeSecret(parts.Take(threshold).ToList(),
                Enumerable.Range(1, threshold).ToList(), threshold);
            Assert.Equal(bytes, result);
        }
        
        [Theory]
        [InlineData(3, 9)]
        [InlineData(12, 17)]
        [InlineData(5, 20)]
        public void HashSharingTest(int threshold, int totalParts)
        {
            var hash = HashHelper.ComputeFrom("hash");
            var hashBytes = hash.ToByteArray();
            var parts = SecretSharingHelper.EncodeSecret(hashBytes, threshold, totalParts);
            var result = SecretSharingHelper.DecodeSecret(parts.Take(threshold).ToList(),
                Enumerable.Range(1, threshold).ToList(), threshold);
            Assert.Equal(hashBytes, result);
        }
    }
}