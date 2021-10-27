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
    public class TransactionExecutionValidationProviderTests : AElfIntegratedTest<TransactionExecutionValidationModule>
    {
        private readonly TransactionExecutionValidationProvider _transactionExecutionValidationProvider;
        private readonly TransactionOptions _transactionOptions;
        private readonly IBlockchainService _blockchainService;
        private readonly KernelTestHelper _kernelTestHelper;
        private readonly ILocalEventBus _eventBus;

        public TransactionExecutionValidationProviderTests()
        {
            _transactionExecutionValidationProvider = GetRequiredService<TransactionExecutionValidationProvider>();
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

            TransactionExecutionValidationFailedEvent transactionExecutionValidationFailedEvent = null;
            _eventBus.Subscribe<TransactionExecutionValidationFailedEvent>(d =>
            {
                transactionExecutionValidationFailedEvent = d;
                return Task.CompletedTask;
            });
            
            var transactionMockExecutionHelper = GetRequiredService<TransactionMockExecutionHelper>();
            transactionMockExecutionHelper.SetTransactionResultStatus(TransactionResultStatus.Mined);
            var kernelTestHelper = GetRequiredService<KernelTestHelper>();
            var transaction = kernelTestHelper.GenerateTransaction();
            
            var result =
                await _transactionExecutionValidationProvider.ValidateTransactionAsync(transaction, await _kernelTestHelper.GetChainContextAsync());
            result.ShouldBeTrue();
            
            transactionValidationStatusChangedEventData.ShouldBeNull();
            transactionExecutionValidationFailedEvent.ShouldBeNull();
        }

        [Theory]
        [InlineData(TransactionResultStatus.Conflict)]
        [InlineData(TransactionResultStatus.NotExisted)]
        [InlineData(TransactionResultStatus.Failed)]
        [InlineData(TransactionResultStatus.Pending)]
        public async Task ValidateTransactionFailedTest(TransactionResultStatus status)
        {
            TransactionValidationStatusChangedEvent transactionValidationStatusChangedEventData = null;
            _eventBus.Subscribe<TransactionValidationStatusChangedEvent>(d =>
            {
                transactionValidationStatusChangedEventData = d;
                return Task.CompletedTask;
            });

            TransactionExecutionValidationFailedEvent transactionExecutionValidationFailedEvent = null;
            _eventBus.Subscribe<TransactionExecutionValidationFailedEvent>(d =>
            {
                transactionExecutionValidationFailedEvent = d;
                return Task.CompletedTask;
            });

            var transactionMockExecutionHelper = GetRequiredService<TransactionMockExecutionHelper>();
            transactionMockExecutionHelper.SetTransactionResultStatus(status);
            var kernelTestHelper = GetRequiredService<KernelTestHelper>();
            var transaction = kernelTestHelper.GenerateTransaction();
            var result =
                await _transactionExecutionValidationProvider.ValidateTransactionAsync(transaction,
                    await _kernelTestHelper.GetChainContextAsync());
            result.ShouldBeFalse();

            transactionValidationStatusChangedEventData.ShouldNotBeNull();
            transactionValidationStatusChangedEventData.TransactionId.ShouldBe(transaction.GetHash());
            transactionValidationStatusChangedEventData.TransactionResultStatus.ShouldBe(TransactionResultStatus
                .NodeValidationFailed);

            transactionExecutionValidationFailedEvent.ShouldNotBeNull();
            transactionExecutionValidationFailedEvent.TransactionId.ShouldBe(transaction.GetHash());
        }

        [Fact]
        public async Task ValidateTransactionTest_DisableTransactionExecutionValidation()
        {
            TransactionValidationStatusChangedEvent transactionValidationStatusChangedEventData = null;
            _eventBus.Subscribe<TransactionValidationStatusChangedEvent>(d =>
            {
                transactionValidationStatusChangedEventData = d;
                return Task.CompletedTask;
            });

            TransactionExecutionValidationFailedEvent transactionExecutionValidationFailedEvent = null;
            _eventBus.Subscribe<TransactionExecutionValidationFailedEvent>(d =>
            {
                transactionExecutionValidationFailedEvent = d;
                return Task.CompletedTask;
            });
            
            _transactionOptions.EnableTransactionExecutionValidation = false;
            var transactionMockExecutionHelper = GetRequiredService<TransactionMockExecutionHelper>();
            transactionMockExecutionHelper.SetTransactionResultStatus(TransactionResultStatus.Failed);
            var kernelTestHelper = GetRequiredService<KernelTestHelper>();
            var transaction = kernelTestHelper.GenerateTransaction();
            var result = await _transactionExecutionValidationProvider.ValidateTransactionAsync(transaction, await _kernelTestHelper.GetChainContextAsync());
            result.ShouldBeTrue();
            
            transactionValidationStatusChangedEventData.ShouldBeNull();
            transactionExecutionValidationFailedEvent.ShouldBeNull();
        }
    }
}