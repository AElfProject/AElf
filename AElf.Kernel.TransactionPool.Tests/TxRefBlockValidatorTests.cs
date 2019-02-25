using System;
using System.Threading.Tasks;
using AElf.Common;
using Moq;
using Shouldly;
using Xunit;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Kernel.TransactionPool.RefBlockExceptions;
using Moq.Language.Flow;

namespace AElf.Kernel.TransactionPool.Tests
{
    public class TxRefBlockValidatorTests:TransactionPoolTestBase
    {
        private readonly ITxRefBlockValidator _validator;
        private const int ChainId = 1234;

        public TxRefBlockValidatorTests()
        {
            _validator = GetRequiredService<ITxRefBlockValidator>();
        }

        [Fact]
        public void Validate_All_Status()
        {
            var transaction = FakeTransaction.Generate();
            _validator.ValidateAsync(ChainId, transaction).ShouldNotThrow();

            transaction.RefBlockNumber = 102;
            _validator.ValidateAsync(ChainId, transaction).ShouldThrow<FutureRefBlockException>();

            transaction.RefBlockNumber = 30;
            _validator.ValidateAsync(ChainId, transaction).ShouldThrow<RefBlockExpiredException>();

            transaction.RefBlockNumber = 90;
            _validator.ValidateAsync(ChainId, transaction).ShouldThrow<Exception>();

            transaction.RefBlockNumber = 80;
            _validator.ValidateAsync(ChainId, transaction).ShouldNotThrow();
        }
    }
}