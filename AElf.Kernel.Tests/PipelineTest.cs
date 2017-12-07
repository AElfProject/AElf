using System;
using Moq;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Tests
{
    public class PipelineTest
    {
        [Fact]
        public void Test()
        {
            var tx = new Mock<ITransaction>();
            var blk = new Mock<IBlock>();
            blk.Setup(b => b.AddTransaction(It.IsAny<ITransaction>())).Returns(true);

        }
    }
}
