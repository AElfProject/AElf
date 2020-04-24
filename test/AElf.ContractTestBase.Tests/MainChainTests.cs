using System.Threading.Tasks;
using AElf.Contracts.Election;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestKit;
using AElf.GovernmentSystem;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.ContractTestBase.Tests
{
    public class MainChainTests : MainChainTestBase
    {
        [Fact]
        public async Task Test()
        {
            var address = ContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name);
            var tokenStub = GetTester<TokenContractContainer.TokenContractStub>(address, SampleECKeyPairs.KeyPairs[0]);
            var balance = await tokenStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[0].PublicKey),
                Symbol = "ELF"
            });
            balance.Balance.ShouldBe(88000000000000000L);

            var electionAddress =
                ContractAddressService.GetAddressByContractName(ElectionSmartContractAddressNameProvider.Name);
            var electionStub = GetTester<ElectionContractContainer.ElectionContractStub>(electionAddress,SampleECKeyPairs.KeyPairs[0]);
            var minerCount = await electionStub.GetMinersCount.CallAsync(new Empty());
            minerCount.Value.ShouldBe(1);
        }
    }
}