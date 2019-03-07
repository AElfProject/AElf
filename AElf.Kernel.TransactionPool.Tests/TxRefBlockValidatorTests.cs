using System;
using Shouldly;
using Xunit;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Kernel.TransactionPool.RefBlockExceptions;

namespace AElf.Kernel.TransactionPool.Tests
{
    public class TxRefBlockValidatorTests : TransactionPoolValidatorTestBase
    {
        private readonly ITxRefBlockValidator _validator;

        public TxRefBlockValidatorTests()
        {
            _validator = GetRequiredService<ITxRefBlockValidator>();
        }

        [Fact]
        public void Validate_All_Status()
        {
            var transaction = FakeTransaction.Generate();
            _validator.ValidateAsync(transaction).ShouldNotThrow();

            transaction.RefBlockNumber = 102;
            _validator.ValidateAsync(transaction).ShouldThrow<FutureRefBlockException>();

            transaction.RefBlockNumber = 30;
            _validator.ValidateAsync(transaction).ShouldThrow<RefBlockExpiredException>();

            transaction.RefBlockNumber = 90;
            _validator.ValidateAsync(transaction).ShouldThrow<Exception>();

            transaction.RefBlockNumber = 80;
            _validator.ValidateAsync(transaction).ShouldNotThrow();
        }
    }
}