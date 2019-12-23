using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace AElf.Kernel.TransactionPool.Application
{
    public class TransactionFromAddressValidationTests : TransactionPoolWithValidationTestBase
    {
        private readonly TransactionFromAddressBalanceValidationProvider _validationProvider;
        private readonly KernelTestHelper _kernelTestHelper;
        public TransactionFromAddressValidationTests()
        {
            _validationProvider = GetRequiredService<TransactionFromAddressBalanceValidationProvider>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
        }

        [Fact]
        public async Task Validation_ValidTx_Test()
        {
            var tx = _kernelTestHelper.GenerateTransaction();
            
            //system tx free
            tx.MethodName = "SystemMethod";
            var validateResult = await _validationProvider.ValidateTransactionAsync(tx);
            validateResult.ShouldBe(true);

            var mockTx = _kernelTestHelper.GenerateTransaction();
            mockTx.From = SampleAddress.AddressList[0];
            validateResult = await _validationProvider.ValidateTransactionAsync(mockTx);
            validateResult.ShouldBe(true);
        }
    }
}