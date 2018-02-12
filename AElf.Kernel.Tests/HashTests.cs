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
        public void Test1()
        {
            var hash = new Mock<IHash>();
            hash.Setup(p => p.GetHashBytes()).Returns(new byte[] {1, 2, 3});

            hash.Object.GetHashBytes()[0].ShouldBe((byte)1);
        }

        [Fact]
        public async Task MerkleTree()
        {
            var mt = new Mock<IMerkleTree<ITransaction>>();

            mt.Setup(p => p.AddNode(It.IsAny<IHash<ITransaction>>()));

            await Task.Delay(1000);
            

        }
    }
}