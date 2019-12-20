using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using Shouldly;
using Xunit;

namespace AElf.Kernel.TransactionPool.Application
{
    public class TransactionMethodNameValidationTests : TransactionPoolWithValidationTestBase
    {
        private readonly TransactionMethodNameValidationProvider _validationProvider;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly KernelTestHelper _kernelTestHelper;
        
        public TransactionMethodNameValidationTests()
        {
            _validationProvider = GetRequiredService<TransactionMethodNameValidationProvider>();
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
        }

        [Fact]
        public async Task Validate_TransactionMethodName_Test()
        {
            var tx = _kernelTestHelper.GenerateTransaction();
            var result =  await _validationProvider.ValidateTransactionAsync(tx);
            result.ShouldBe(true);

            _smartContractAddressService.SetAddress(TokenSmartContractAddressNameProvider.Name,
                SampleAddress.AddressList.Last());
            tx.To = SampleAddress.AddressList.Last();
            tx.MethodName = "ChargeTransactionFees";
            result =  await _validationProvider.ValidateTransactionAsync(tx);
            result.ShouldBe(false);
            
            tx.MethodName = "Transfer";
            result =  await _validationProvider.ValidateTransactionAsync(tx);
            result.ShouldBe(true);
        }
    }
}