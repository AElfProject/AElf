using System;
using System.Linq;
using System.Net.Mime;
using System.Numerics;
using System.Text;
using AElf.Cryptography.SecretSharing;
using AElf.Types;
using Shouldly;
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
        [InlineData("gggaelf", 3, 9)]
        [InlineData("bbbaelf", 12, 17)]
        [InlineData("kkkkaelf", 5, 20)]
        public void SharingTest(string str, int threshold, int totalParts)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            ICodeExcutor excutor = new SecretSharingExcutor();
            var parts = excutor.EncodeSecret(bytes, threshold, totalParts);
            Assert.Equal(totalParts, parts.Count);
            IRationalHelper irh = new SecretSharingMathHelper();
            var result = excutor.DecodeSecret(parts.Take(threshold).ToList(),
                Enumerable.Range(1, threshold).ToList(), threshold,irh);
            Assert.Equal(bytes, result);
        }
        
        [Theory]
        [InlineData(3, 9)]
        [InlineData(12, 17)]
        [InlineData(5, 20)]
        public void HashSharingTest(int threshold, int totalParts)
        {
            var hash = Hash.FromString("hash");
            var hashBytes = hash.ToByteArray();
            ICodeExcutor excutor = new SecretSharingExcutor();
            var parts = excutor.EncodeSecret(hashBytes, threshold, totalParts);
            IRationalHelper irh = new SecretSharingMathHelper();
            var result = excutor.DecodeSecret(parts.Take(threshold).ToList(),
                Enumerable.Range(1, threshold).ToList(), threshold,irh);
            Assert.Equal(hashBytes, result);
        }
    }
}