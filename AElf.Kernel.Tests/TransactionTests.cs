using System;
using Moq;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Tests
{
    public class TransactionTests
    {
        [Fact]
        public void TransactionTests()
        {
            var tx = new Mock<ITransaction>();
        }
    }
}
