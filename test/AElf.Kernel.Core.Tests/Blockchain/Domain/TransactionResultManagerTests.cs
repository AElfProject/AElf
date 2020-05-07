using System.Threading.Tasks;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Blockchain.Domain
{
    public class TransactionResultManagerTests : AElfKernelTestBase
    {
        
        private readonly ITransactionResultManager _transactionResultManager;
        private KernelTestHelper _kernelTestHelper;

        public TransactionResultManagerTests()
        {
            _transactionResultManager = GetRequiredService<ITransactionResultManager>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();

        }
        [Fact]
        private async Task AddTransactionResultAsync_Tests()
        {
            
            var tran = _kernelTestHelper.GenerateTransaction();
            var tranResult = _kernelTestHelper.GenerateTransactionResult(tran, TransactionResultStatus.Mined);
            var disambiguationHash1 = HashHelper.ComputeFrom("disambiguationHash1");
            await _transactionResultManager.AddTransactionResultAsync(tranResult, disambiguationHash1);

            var tranResult2 =
                await _transactionResultManager.GetTransactionResultAsync(tranResult.TransactionId, disambiguationHash1);
            tranResult.TransactionId.ShouldBe(tranResult2.TransactionId);

        }
    }
}