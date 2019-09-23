using System.Threading.Tasks;
using AElf.Contracts.Economic;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.MultiToken;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using AElf.Contracts.Election;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using Volo.Abp.Threading;

namespace AElf.Contracts.EconomicSystem.Tests.BVT
{
    public class TokenTestBase : EconomicSystemTestBase
    {
        protected ECKeyPair AnnounceElectionKeyPair => SampleECKeyPairs.KeyPairs[81];
        internal ElectionContractContainer.ElectionContractStub tokenTestElectionContractStub { get; set; }
    }

    public class TokenTest : TokenTestBase
    {
        public TokenTest()
        {
            InitializeContracts();
            tokenTestElectionContractStub =
                GetTester<ElectionContractContainer.ElectionContractStub>(ElectionContractAddress,
                    AnnounceElectionKeyPair);

            var issueResult = AsyncHelper.RunSync(() => EconomicContractStub.IssueNativeToken.SendAsync(
                new IssueNativeTokenInput
                {
                    Amount = 1000_000_00000000L,
                    To = Address.FromPublicKey(AnnounceElectionKeyPair.PublicKey),
                    Memo = "Used to transfer other testers"
                }));
            issueResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            {
                var result = AsyncHelper.RunSync(()=> EconomicContractStub.IssueNativeToken.SendAsync(new IssueNativeTokenInput
                {
                    Amount = EconomicContractsTestConstants.TotalSupply / 5,
                    To = TokenContractAddress,
                    Memo = "Set mining rewards."
                }));
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
        }

        private async Task Token_Lock_Test()
        {
            await tokenTestElectionContractStub.AnnounceElection.SendAsync(new Empty());
            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = Address.FromPublicKey(AnnounceElectionKeyPair.PublicKey),
                Symbol = EconomicSystemTestConstants.NativeTokenSymbol
            });
            balance.Balance.ShouldBe(1000_000_00000000 - 100_000_00000000);
        }

        [Fact]
        public async Task Token_Unlock_Test()
        {
            await Token_Lock_Test();
            
            await tokenTestElectionContractStub.QuitElection.SendAsync(new Empty());
            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = Address.FromPublicKey(AnnounceElectionKeyPair.PublicKey),
                Symbol = EconomicSystemTestConstants.NativeTokenSymbol
            });
            balance.Balance.ShouldBe(1000_000_00000000L);
        }

        [Fact]
        public async Task SetResourceTokenUnitPrice_Test()
        {
            var result = await TokenContractStub.SetResourceTokenUnitPrice.SendAsync(
                new SetResourceTokenUnitPriceInput()
                {
                    NetUnitPrice = 10L,
                    CpuUnitPrice = 10L,
                    StoUnitPrice = 10L
                });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
    }
}