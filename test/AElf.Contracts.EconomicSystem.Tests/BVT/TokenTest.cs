using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Economic;
using AElf.Contracts.MultiToken.Messages;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using AElf.Contracts.Election;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using Google.Protobuf;
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
        }

        [Fact]
        public async Task Token_Lock()
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
        public async Task Token_Unlock()
        {
            Token_Lock();
            var address = AnnounceElectionKeyPair.PublicKey;
            await tokenTestElectionContractStub.QuitElection.SendAsync(new Empty());
            var balance = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = Address.FromPublicKey(AnnounceElectionKeyPair.PublicKey),
                Symbol = EconomicSystemTestConstants.NativeTokenSymbol
            });
            balance.Balance.ShouldBe(1000_000_00000000L);
        }

        [Fact]
        public async Task SetResourceTokenUnitPrice()
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
        
        [Fact(DisplayName = "[MultiToken] MultiToken_ChargeTransactionFees_Test")]
        public async Task MultiTokenContract_ChargeTransactionFees()
        {
            var result = (await TokenContractStub.ChargeTransactionFees.SendAsync(new ChargeTransactionFeesInput
            {
                SymbolToAmount = {new Dictionary<string, long> {{EconomicSystemTestConstants.NativeTokenSymbol, 10L}}}
            })).TransactionResult;
            result.Status.ShouldBe(TransactionResultStatus.Mined);
            await TokenContractStub.Transfer.SendAsync(new TransferInput
            {
                Symbol = EconomicSystemTestConstants.NativeTokenSymbol,
                Amount = 1000L,
                Memo = "transfer test",
                To = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[0].PublicKey)
            });
            var balanceOutput = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = BootMinerAddress,
                Symbol = EconomicSystemTestConstants.NativeTokenSymbol
            });
            balanceOutput.Balance.ShouldBe(100_000_000L - 1000L - 10L);
        }

        [Fact]
        public async Task Claim_Transaction_Fees()
        {
            var originBalanceOutput1 = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TreasuryContractAddress,
                Symbol = EconomicSystemTestConstants.NativeTokenSymbol
            });
            originBalanceOutput1.Balance.ShouldBe(0L);
            await MultiTokenContract_ChargeTransactionFees();

            var originBalanceOutput = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TreasuryContractAddress,
                Symbol = EconomicSystemTestConstants.NativeTokenSymbol
            });
            originBalanceOutput.Balance.ShouldBe(0L);

            {
                var result = (await TokenContractStub.ClaimTransactionFees.SendAsync(new Empty()
                )).TransactionResult;
                result.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            var balanceOutput = await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = TreasuryContractAddress,
                Symbol =EconomicSystemTestConstants.NativeTokenSymbol
            });
            balanceOutput.Balance.ShouldBe(10L);

        }
    }
}