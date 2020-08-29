using System.Collections.Generic;
using Xunit;
using Shouldly;

namespace AElf.Types.Tests
{
    public class HashTests
    {
        [Fact]
        public void Generate_Hash_Test()
        {
            //Generate randomly
            var hash1 = HashHelper.ComputeFrom("hash1");
            var hash2 = HashHelper.ComputeFrom("hash2");
            hash1.ShouldNotBe(hash2);

            //Generate from string
            var hash3 = HashHelper.ComputeFrom("Test");
            hash3.ShouldNotBe(null);

            //Generate from byte
            var bytes = new byte[]{00, 12, 14, 16};
            var hash4 = HashHelper.ComputeFrom(bytes);
            hash4.ShouldNotBe(null);

            //Generate from teo hash
            var hash5 = HashHelper.ConcatAndCompute(hash1, hash2);
            hash5.ShouldNotBe(null);
            
            //Generate from xor
            var hash6 = HashHelper.XorAndCompute(hash1, hash2);
            hash6.ShouldNotBe(null);

            //Generate from long
            long longtype = 1;
            var hash7 = HashHelper.ComputeFrom(longtype);
            hash7.ShouldNotBe(null);
            
            //Generate from Message
            var message= new Transaction();
            var hash8 = HashHelper.ComputeFrom(message);
            hash8.ShouldNotBe(null);
        }

        [Fact]
        public void Get_Hash_Info_Test()
        {
            var hash = HashHelper.ComputeFrom("hash");
            var byteArray = hash.ToByteArray();
            var hexString = hash.ToHex();
            byteArray.Length.ShouldBe(32);
            hexString.ShouldNotBe(string.Empty);
        }

        [Fact]
        public void Equal_Test()
        {
            var hash1 = HashHelper.ComputeFrom(new byte[] {10, 14, 1, 15});
            var hash2 = HashHelper.ComputeFrom(new byte[] {10, 14, 1, 15});
            var hash3 = HashHelper.ComputeFrom(new byte[] {15, 1, 14, 10});
            hash1.ShouldBe(hash2);
            hash1.ShouldNotBe(hash3);
        }

        [Fact]
        public void Compare_Test()
        {
            var hash1 = HashHelper.ComputeFrom(new byte[] {10, 14, 1, 15});
            var hash2 = HashHelper.ComputeFrom(new byte[] {15, 1, 14, 10});
            hash1.CompareTo(hash2).ShouldBe(-1); 
            Should.Throw<System.InvalidOperationException>(() => { hash1.CompareTo(null); });
            
            (hash1 < null).ShouldBeFalse();
            (null < hash2).ShouldBeTrue();
            (hash1 > hash2).ShouldBe(hash1.CompareTo(hash2)>0);

            Hash hashA = null;
            Hash hashB = null;
            var value = hashA > hashB;
            value.ShouldBeFalse();
        }

        [Fact]
        public void Dictionary_Test()
        {
            var dict = new Dictionary<Hash, string>();
            var hash = HashHelper.ComputeFrom(new byte[] {10, 14, 1, 15});
            dict[hash] = "test";

            var anotherHash = HashHelper.ComputeFrom(new byte[] {10, 14, 1, 15});

            Assert.True(dict.TryGetValue(anotherHash, out var test));
            test.ShouldBe("test");
        }
    }
}