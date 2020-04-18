using System.Threading.Tasks;
using AElf.Contracts.CrossChain;
using AElf.Contracts.TestKit;
using AElf.CrossChain;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.ContractTestBase.Tests
{
    public class SideChainTests : SideChainTestBase
    {
        [Fact]
        public async Task Test()
        {
            var address = ContractAddressService.GetAddressByContractName(CrossChainSmartContractAddressNameProvider.Name);
            var crossChainStub = GetTester<CrossChainContractContainer.CrossChainContractStub>(address, SampleECKeyPairs.KeyPairs[0]);
            var parentChainId = await crossChainStub.GetParentChainId.CallAsync(new Empty());
            ChainHelper.ConvertChainIdToBase58(parentChainId.Value).ShouldBe("AELF");
        }
    }
}