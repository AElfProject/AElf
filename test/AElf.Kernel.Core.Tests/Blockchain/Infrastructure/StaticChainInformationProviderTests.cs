using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Blockchain.Infrastructure
{
    public class StaticChainInformationProviderTests : AElfKernelTestBase
    {
        private readonly IStaticChainInformationProvider _staticChainInformationProvider;
        private readonly IChainManager _chainManager;
            
        public StaticChainInformationProviderTests()
        {
            _staticChainInformationProvider = GetRequiredService<IStaticChainInformationProvider>();
            _chainManager = GetRequiredService<IChainManager>();
        }

        [Fact]
        public void GetZeroSmartContractAddress_Test()
        {
            var chainId = _chainManager.GetChainId();
            var address = _staticChainInformationProvider.GetZeroSmartContractAddress(chainId);
            var address1 = BuildZeroContractAddress(chainId);
            address.ShouldBe(address1);
        }
        private static Address BuildZeroContractAddress(int chainId)
        {
            var hash = HashHelper.ConcatAndCompute(HashHelper.ComputeFrom(chainId), HashHelper.ComputeFrom(0L));
            return Address.FromBytes(hash.ToByteArray());
        }
    }
}