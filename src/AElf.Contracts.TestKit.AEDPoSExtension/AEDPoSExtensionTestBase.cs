using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.TestKit;
using AElf.Kernel.Consensus;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable InconsistentNaming
namespace AElf.Contracts.TestKet.AEDPoSExtension
{
    public class AEDPoSExtensionTestBase : ContractTestBase<ContractTestAEDPoSExtensionModule>
    {
        protected IBlockMiningService BlockMiningService =>
            Application.ServiceProvider.GetRequiredService<IBlockMiningService>();

        internal AEDPoSContractImplContainer.AEDPoSContractImplStub ConsensusStub =>
            GetTester<AEDPoSContractImplContainer.AEDPoSContractImplStub>(
                ContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider.Name),
                SampleECKeyPairs.KeyPairs[0]);
    }
}