using System.Threading.Tasks;
using AElf.Contracts.CrossChain;
using AElf.ContractTestKit;
using AElf.CrossChain;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.ContractTestBase.Tests
{
    public class SideChainTests : SideChainTestBase
    {
        private readonly IBlockchainService _blockchainService;

        public SideChainTests()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
        }

        [Fact]
        public async Task Test()
        {
            var preBlockHeader = await _blockchainService.GetBestChainLastBlockHeaderAsync();
            var chainContext = new ChainContext
            {
                BlockHash = preBlockHeader.GetHash(),
                BlockHeight = preBlockHeader.Height
            };
            var contractMapping = await ContractAddressService.GetSystemContractNameToAddressMappingAsync(chainContext);

            var crossChainStub = GetTester<CrossChainContractContainer.CrossChainContractStub>(
                contractMapping[CrossChainSmartContractAddressNameProvider.Name], Accounts[0].KeyPair);
            var parentChainId = await crossChainStub.GetParentChainId.CallAsync(new Empty());
            ChainHelper.ConvertChainIdToBase58(parentChainId.Value).ShouldBe("AELF");
        }
    }
}