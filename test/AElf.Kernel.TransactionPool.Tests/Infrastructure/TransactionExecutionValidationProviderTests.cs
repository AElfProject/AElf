using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.TestBase;
using AElf.Types;
using Microsoft.Extensions.Options;
using Shouldly;
using Xunit;

namespace AElf.Kernel.TransactionPool.Infrastructure
{
    public class TransactionExecutionValidationProviderTests : AElfIntegratedTest<TransactionExecutionValidationModule>
    {
        private readonly TransactionExecutionValidationProvider _transactionExecutionValidationProvider;
        private readonly TransactionOptions _transactionOptions;
        private readonly IBlockchainService _blockchainService;
        private readonly KernelTestHelper _kernelTestHelper;

        public TransactionExecutionValidationProviderTests()
        {
            _transactionExecutionValidationProvider = GetRequiredService<TransactionExecutionValidationProvider>();
            _transactionOptions = GetRequiredService<IOptionsMonitor<TransactionOptions>>().CurrentValue;
            _blockchainService = GetRequiredService<IBlockchainService>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
        }

        [Fact]
        public async Task ValidateTransactionTest()
        {
            var transactionMockExecutionHelper = GetRequiredService<TransactionMockExecutionHelper>();
            transactionMockExecutionHelper.SetTransactionResultStatus(TransactionResultStatus.Mined);
            var kernelTestHelper = GetRequiredService<KernelTestHelper>();
            var transaction = kernelTestHelper.GenerateTransaction();
            
            var result =
                await _transactionExecutionValidationProvider.ValidateTransactionAsync(transaction, await _kernelTestHelper.GetChainContextAsync());
            result.ShouldBeTrue();
        }
        
        [Theory]
        [InlineData(TransactionResultStatus.Conflict)]
        [InlineData(TransactionResultStatus.NotExisted)]
        [InlineData(TransactionResultStatus.Failed)]
        [InlineData(TransactionResultStatus.Unexecutable)]
        [InlineData(TransactionResultStatus.Pending)]
        public async Task ValidateTransactionFailedTest(TransactionResultStatus status)
        {
            var transactionMockExecutionHelper = GetRequiredService<TransactionMockExecutionHelper>();
            transactionMockExecutionHelper.SetTransactionResultStatus(status);
            var kernelTestHelper = GetRequiredService<KernelTestHelper>();
            var transaction = kernelTestHelper.GenerateTransaction();
            var result = await _transactionExecutionValidationProvider.ValidateTransactionAsync(transaction, await _kernelTestHelper.GetChainContextAsync());
            result.ShouldBeFalse();
        }
        
        [Fact]
        public async Task ValidateTransactionTest_DisableTransactionExecutionValidation()
        {
            _transactionOptions.EnableTransactionExecutionValidation = false;
            var transactionMockExecutionHelper = GetRequiredService<TransactionMockExecutionHelper>();
            transactionMockExecutionHelper.SetTransactionResultStatus(TransactionResultStatus.Failed);
            var kernelTestHelper = GetRequiredService<KernelTestHelper>();
            var transaction = kernelTestHelper.GenerateTransaction();
            var result = await _transactionExecutionValidationProvider.ValidateTransactionAsync(transaction, await _kernelTestHelper.GetChainContextAsync());
            result.ShouldBeTrue();
        }
    }
}