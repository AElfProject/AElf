using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using AElf.Kernel.Merkle;
using System.Threading.Tasks;
using Moq;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Tests
{
    public class HashTests
    {

        [Fact]
        public async Task MerkleTree()
        {
            var mt = new Mock<IMerkleTree>();

            mt.Setup(p => p.AddNode(It.IsAny<IHash>()));

            await Task.Delay(1000);
            

        }

        [Fact]
        public void BasicTest()
        {
            var hash1 = new Hash(new byte[] {10, 14, 1, 15});
            var hash2 = new Hash(new byte[] {10, 14, 1, 15});
            Assert.True(hash1 == hash2);
        }

        [Fact]
        public void DictionaryTest()
        {
            var dict = new Dictionary<Hash, string>();
            var hash = new Hash(new byte[] {10, 14, 1, 15});
            dict[hash] = "test";
            
            var anotherHash = new Hash(new byte[] {10, 14, 1, 15});
            
            Assert.True(dict.TryGetValue(anotherHash, out var test));
            Assert.Equal(test, "test");
        }
        
    }
}