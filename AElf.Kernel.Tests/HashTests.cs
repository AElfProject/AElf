using System;
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
            hash.Setup(p => p.GetBytes()).Returns(new byte[] {1, 2, 3});

            hash.Object.GetBytes()[0].ShouldBe((byte)1);
        }

        [Fact]
        public void MerkleTree()
        {
            var mt=new Mock<IMerkleTree<ITransaction>>();

            mt.Setup(p => p.AddNode(It.IsAny<IHash<ITransaction>>()));
        }
    }
}