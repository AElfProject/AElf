using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using Shouldly;
using Xunit;

namespace AElf.Kernel.TransactionPool.Application
{
    public class TransactionNotAllowEnterTxHubTests : TransactionPoolWithValidationTestBase
    {
        private readonly ExcludeFromTxHubValidationProvider _validationProvider;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly KernelTestHelper _kernelTestHelper;

        public TransactionNotAllowEnterTxHubTests()
        {
            _validationProvider = GetRequiredService<ExcludeFromTxHubValidationProvider>();
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
        }

        [Fact]
        public async Task ValidateTransaction_Test()
        {
            var tx = _kernelTestHelper.GenerateTransaction();
            var result =  await _validationProvider.ValidateTransactionAsync(tx);
            result.ShouldBe(true);
            
            _smartContractAddressService.SetAddress(TokenSmartContractAddressNameProvider.Name,
                SampleAddress.AddressList.Last());
            tx.To = SampleAddress.AddressList.Last();
            tx.MethodName = "ClaimTransactionFees";
            result =  await _validationProvider.ValidateTransactionAsync(tx);
            result.ShouldBe(false);
        }
    }
}