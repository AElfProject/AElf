using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Consensus.AEDPoS;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Txn.Application;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Consensus.DPoS.Tests.Application
{
    public class TxHubEntryPermissionValidationProviderTests : AEDPoSTestBase
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITransactionValidationProvider _validationProvider;
        private readonly KernelTestHelper _kernelTestHelper;
        public TxHubEntryPermissionValidationProviderTests()
        {
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
            _validationProvider = GetRequiredService<ITransactionValidationProvider>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
        }

        [Fact]
        public async Task Validate_EconomicAddress_Test()
        {
            var tx = _kernelTestHelper.GenerateTransaction();
            var economicAddress =
                _smartContractAddressService.GetAddressByContractName(EconomicSmartContractAddressNameProvider.Name);
            tx.To = economicAddress;
            var result = await _validationProvider.ValidateTransactionAsync(tx);
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task Validate_ConsensusAddress_Test()
        {
            var tx = _kernelTestHelper.GenerateTransaction();
            tx.To = SampleAddress.AddressList.Last();
            var result = await _validationProvider.ValidateTransactionAsync(tx);
            result.ShouldBeTrue();
            
            var consensusAddress =
                _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider.Name);
            tx.To = consensusAddress;
            tx.MethodName = "UpdateValue";
            result = await _validationProvider.ValidateTransactionAsync(tx);
            result.ShouldBeFalse();
        }
    }
}