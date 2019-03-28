using System.Linq;
using System.Numerics;
using AElf.Common;
using AElf.Cryptography.SecretSharing;
using Xunit;

namespace AElf.Cryptography.Tests
{
    public class SecretSharingTest
    {
        [Fact]
        public void BigIntegerToStringTest()
        {
            var bigInteger =
                BigInteger.Parse("68117399500852794112165623985513434038399476516881142682654290358811497358689");
            var str = bigInteger.ConvertToString();
            Assert.Equal("aelf", str);
        }

        [Theory]
        [InlineData("aelf", 3, 9)]
        [InlineData("aelf", 12, 17)]
        [InlineData("aelf", 5, 20)]
        public void SharingTest(string str, int threshold, int totalParts)
        {
            var parts = SecretSharingHelper.EncodeSecret(str, threshold, totalParts);
            Assert.Equal(totalParts, parts.Count);

            var result = SecretSharingHelper.DecodeSecret(parts.Take(threshold).ToList(),
                Enumerable.Range(1, threshold).ToList(), threshold);
            Assert.Equal(str, result);
        }
        
        [Theory]
        [InlineData(3, 9)]
        [InlineData(12, 17)]
        [InlineData(5, 20)]
        public void HashSharingTest(int threshold, int totalParts)
        {
            var hash = Hash.Generate().ToHex();
            var parts = SecretSharingHelper.EncodeSecret(hash, threshold, totalParts);
            var result = SecretSharingHelper.DecodeSecret(parts.Take(threshold).ToList(),
                Enumerable.Range(1, threshold).ToList(), threshold);
            Assert.Equal(hash, result);
        }
    }
}