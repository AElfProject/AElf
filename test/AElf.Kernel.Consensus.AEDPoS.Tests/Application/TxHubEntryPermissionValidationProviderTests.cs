using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
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
        private readonly IBlockchainService _blockchainService;
        public TxHubEntryPermissionValidationProviderTests()
        {
            _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
            _validationProvider = GetRequiredService<ITransactionValidationProvider>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
            _blockchainService = GetRequiredService<IBlockchainService>();
        }

        [Fact]
        public async Task Validate_EconomicAddress_Test()
        {
            var tx = _kernelTestHelper.GenerateTransaction();
            var chainContext = await _kernelTestHelper.GetChainContextAsync();
            var economicAddress =
                await _smartContractAddressService.GetAddressByContractNameAsync(chainContext, EconomicSmartContractAddressNameProvider.StringName);
            tx.To = economicAddress;
            var result = await _validationProvider.ValidateTransactionAsync(tx, chainContext);
            result.ShouldBeFalse();
        }

        [Fact]
        public async Task Validate_ConsensusAddress_Test()
        {
            var tx = _kernelTestHelper.GenerateTransaction();
            tx.To = SampleAddress.AddressList.Last();
            var chainContext = await _kernelTestHelper.GetChainContextAsync();
            var result = await _validationProvider.ValidateTransactionAsync(tx,chainContext);
            result.ShouldBeTrue();
            
            var consensusAddress =
                await _smartContractAddressService.GetAddressByContractNameAsync(chainContext, ConsensusSmartContractAddressNameProvider.StringName);
            tx.To = consensusAddress;
            tx.MethodName = "UpdateValue";
            result = await _validationProvider.ValidateTransactionAsync(tx,chainContext);
            result.ShouldBeFalse();
        }
    }
}