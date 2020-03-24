using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee.Tests
{
    public class TransactionFromAddressValidationTests : ExecutionPluginForMethodFeeTestBase
    {
        private readonly MethodFeeAffordableValidationProvider _validationProvider;
        private readonly KernelTestHelper _kernelTestHelper;
        public TransactionFromAddressValidationTests()
        {
            _validationProvider = GetRequiredService<MethodFeeAffordableValidationProvider>();
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
        }
    }
}