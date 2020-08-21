using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.TestBase;
using AElf.Types;
using Microsoft.Extensions.Options;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AElf.Kernel.TransactionPool.Infrastructure
{
    public class TransactionMethodValidationProviderTests : AElfIntegratedTest<TransactionExecutionValidationModule>
    {
        private readonly TransactionMethodValidationProvider _transactionMethodValidationProvider;
        private readonly TransactionOptions _transactionOptions;
        private readonly IBlockchainService _blockchainService;
        private readonly KernelTestHelper _kernelTestHelper;
        private readonly ILocalEventBus _eventBus;

        public TransactionMethodValidationProviderTests()
        {
            _transactionMethodValidationProvider = GetRequiredService<TransactionMethodValidationProvider>();
            _transactionOptions = GetRequiredService<IOptionsMonitor<TransactionOptions>>().CurrentValue;
            _blockchainService = GetRequiredService<IBlockchainService>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
            _eventBus = GetRequiredService<ILocalEventBus>();
        }

        [Fact]
        public async Task ValidateTransactionTest()
        {
            TransactionValidationStatusChangedEvent transactionValidationStatusChangedEventData = null;
            _eventBus.Subscribe<TransactionValidationStatusChangedEvent>(d =>
            {
                transactionValidationStatusChangedEventData = d;
                return Task.CompletedTask;
            });
            
            var kernelTestHelper = GetRequiredService<KernelTestHelper>();
            var transaction = kernelTestHelper.GenerateTransaction();
            
            var result =
                await _transactionMethodValidationProvider.ValidateTransactionAsync(transaction, await _kernelTestHelper.GetChainContextAsync());
            result.ShouldBeTrue();
            
            transactionValidationStatusChangedEventData.ShouldBeNull();

            transaction.MethodName = "View";
            
            result =
                await _transactionMethodValidationProvider.ValidateTransactionAsync(transaction, await _kernelTestHelper.GetChainContextAsync());
            result.ShouldBeFalse();
            
            transactionValidationStatusChangedEventData.ShouldNotBeNull();
            transactionValidationStatusChangedEventData.TransactionResultStatus.ShouldBe(TransactionResultStatus.NodeValidationFailed);
            transactionValidationStatusChangedEventData.Error.ShouldBe("View transaction is not allowed.");
            
        }
    }
}