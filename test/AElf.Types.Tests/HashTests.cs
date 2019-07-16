using System.Collections.Generic;
using Xunit;
using Shouldly;

namespace AElf.Types.Tests
{
    public class HashTests
    {

        [Fact]
        public void Generate_Hash()
        {
            //Generate randomly
            var hash1 = Hash.FromString("hash1");
            var hash2 = Hash.FromString("hash2");
            hash1.ShouldNotBe(hash2);

            //Generate from string
            var hash3 = Hash.FromString("Test");
            hash3.ShouldNotBe(null);

            //Generate from byte
            var bytes = new byte[]{00, 12, 14, 16};
            var hash4 = Hash.FromRawBytes(bytes);
            hash4.ShouldNotBe(null);

            //Generate from teo hash
            var hash5 = Hash.FromTwoHashes(hash1, hash2);
            hash5.ShouldNotBe(null);
            
            //Generate from xor
            var hash6 = HashHelper.Xor(hash1, hash2);
            hash6.ShouldNotBe(null);
        }

        [Fact]
        public void Get_Hash_Info()
        {
            var hash = Hash.FromString("hash");
            var byteArray = hash.ToByteArray();
            var hexString = hash.ToHex();
            byteArray.Length.ShouldBe(32);
            hexString.ShouldNotBe(string.Empty);
        }

        [Fact]
        public void EqualTest()
        {
            var hash1 = Hash.FromRawBytes(new byte[] {10, 14, 1, 15});
            var hash2 = Hash.FromRawBytes(new byte[] {10, 14, 1, 15});
            var hash3 = Hash.FromRawBytes(new byte[] {15, 1, 14, 10});
            hash1.ShouldBe(hash2);
            hash1.ShouldNotBe(hash3);
        }

        [Fact]
        public void CompareTest()
        {
            var hash1 = Hash.FromRawBytes(new byte[] {10, 14, 1, 15});
            var hash2 = Hash.FromRawBytes(new byte[] {15, 1, 14, 10});
            hash1.CompareTo(hash2).ShouldBe(-1); 
            Should.Throw<System.InvalidOperationException>(() => { hash1.CompareTo(null); });
            
            (hash1 < null).ShouldBeFalse();
            (null < hash2).ShouldBeTrue();
            (hash1 > hash2).ShouldBe(hash1.CompareTo(hash2)>0);
        }

        [Fact]
        public void DictionaryTest()
        {
            var dict = new Dictionary<Hash, string>();
            var hash = Hash.FromRawBytes(new byte[] {10, 14, 1, 15});
            dict[hash] = "test";

            var anotherHash = Hash.FromRawBytes(new byte[] {10, 14, 1, 15});

            Assert.True(dict.TryGetValue(anotherHash, out var test));
            test.ShouldBe("test");
        }
    }
}