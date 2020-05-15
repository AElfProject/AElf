using System.Threading.Tasks;
using Acs10;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestContract.TransactionFeeCharging;
using AElf.Contracts.TestKit;
using AElf.Contracts.TokenConverter;
using AElf.Contracts.Treasury;
using AElf.Cryptography.ECDSA;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.EconomicSystem.Tests.BVT
{
    public partial class EconomicSystemTest
    {
        [Fact]
        public async Task Donate_FewELF_Success_Test()
        {
            var keyPair = SampleECKeyPairs.KeyPairs[1];
            await TransferToken(keyPair, EconomicSystemTestConstants.NativeTokenSymbol, 100);
            var stub = GetTreasuryContractTester(keyPair);

            var donateResult = await stub.Donate.SendAsync(new DonateInput
            {
                Symbol = EconomicSystemTestConstants.NativeTokenSymbol,
                Amount = 50
            });
            donateResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var userBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = Address.FromPublicKey(keyPair.PublicKey),
                Symbol = EconomicSystemTestConstants.NativeTokenSymbol
            })).Balance;
            userBalance.ShouldBe(50);

            var treasuryBalance = await GetCurrentTreasuryBalance();
            treasuryBalance.ShouldBeGreaterThanOrEqualTo(50);
        }

        [Fact]
        public async Task Donate_AllELF_Success_Test()
        {
            var keyPair = SampleECKeyPairs.KeyPairs[1];
            await TransferToken(keyPair, EconomicSystemTestConstants.NativeTokenSymbol, 100);
            var stub = GetTreasuryContractTester(keyPair);

            var donateResult = await stub.DonateAll.SendAsync(new DonateAllInput
            {
                Symbol = EconomicSystemTestConstants.NativeTokenSymbol
            });
            donateResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var userBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = Address.FromPublicKey(keyPair.PublicKey),
                Symbol = EconomicSystemTestConstants.NativeTokenSymbol
            })).Balance;
            userBalance.ShouldBe(0);

            var treasuryBalance = await GetCurrentTreasuryBalance();
            treasuryBalance.ShouldBeGreaterThanOrEqualTo(100);
        }

        [Fact]
        public async Task Donate_ELF_LessThan_Owned_Test()
        {
            var keyPair = SampleECKeyPairs.KeyPairs[1];

            await TransferToken(keyPair, EconomicSystemTestConstants.NativeTokenSymbol, 50);
            var stub = GetTreasuryContractTester(keyPair);
            var donateResult = await stub.Donate.SendAsync(new DonateInput
            {
                Symbol = EconomicSystemTestConstants.NativeTokenSymbol,
                Amount = 100
            });
            donateResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);

            var userBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = Address.FromPublicKey(keyPair.PublicKey),
                Symbol = EconomicSystemTestConstants.NativeTokenSymbol
            })).Balance;
            userBalance.ShouldBe(50);
        }

        [Fact]
        public async Task Donate_FewOtherToken_Success_Test()
        {
            await InitialBuildConnector(EconomicSystemTestConstants.TransactionFeeChargingContractTokenSymbol);
            var keyPair = CoreDataCenterKeyPairs[0];
            await TransferToken(keyPair, EconomicSystemTestConstants.TransactionFeeChargingContractTokenSymbol, 100);
            var stub = GetTreasuryContractTester(keyPair);
            var donateResult = await stub.Donate.SendAsync(new DonateInput
            {
                Symbol = EconomicSystemTestConstants.TransactionFeeChargingContractTokenSymbol,
                Amount = 50
            });
            donateResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var userBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = Address.FromPublicKey(keyPair.PublicKey),
                Symbol = EconomicSystemTestConstants.TransactionFeeChargingContractTokenSymbol
            })).Balance;
            userBalance.ShouldBe(50);
        }

        private async Task InitialBuildConnector(string symbol)
        {
            var token = (await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput
            {
                Symbol = symbol,
            }));
            var tokenInfo = new ToBeConnectedTokenInfo
            {
                TokenSymbol = symbol,
                AmountToTokenConvert = 0
            };
            var issueRet = (await TransactionFeeChargingContractStub.IssueToTokenConvert.SendAsync(
                new IssueAmount
                {
                    Symbol = symbol,
                    Amount = token.TotalSupply - token.Supply
                })).TransactionResult;
            issueRet.Status.ShouldBe(TransactionResultStatus.Mined);
            var buildConnector = (await TokenConverterContractStub.EnableConnector.SendAsync(tokenInfo)).TransactionResult;
            buildConnector.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        [Fact]
        public async Task Donate_AllOtherToken_Success_Test()
        {
            await InitialBuildConnector(EconomicSystemTestConstants.TransactionFeeChargingContractTokenSymbol);
            var keyPair = CoreDataCenterKeyPairs[0];

            await TransferToken(keyPair, EconomicSystemTestConstants.TransactionFeeChargingContractTokenSymbol, 100);
            var stub = GetTreasuryContractTester(keyPair);
            var donateResult = await stub.DonateAll.SendAsync(new DonateAllInput
            {
                Symbol = EconomicSystemTestConstants.TransactionFeeChargingContractTokenSymbol
            });
            donateResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var userBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = Address.FromPublicKey(keyPair.PublicKey),
                Symbol = EconomicSystemTestConstants.TransactionFeeChargingContractTokenSymbol
            })).Balance;
            userBalance.ShouldBe(0);
        }

        [Fact]
        public async Task Donate_OtherToken_LessThan_Owned_Test()
        {
            await InitialBuildConnector(EconomicSystemTestConstants.TransactionFeeChargingContractTokenSymbol);
            var keyPair = CoreDataCenterKeyPairs[0];
            
            await TransferToken(keyPair, EconomicSystemTestConstants.TransactionFeeChargingContractTokenSymbol, 50);
            var stub = GetTreasuryContractTester(keyPair);
            var donateResult = await stub.Donate.SendAsync(new DonateInput
            {
                Symbol = EconomicSystemTestConstants.TransactionFeeChargingContractTokenSymbol,
                Amount = 100
            });
            donateResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);

            var userBalance = (await TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Owner = Address.FromPublicKey(keyPair.PublicKey),
                Symbol = EconomicSystemTestConstants.TransactionFeeChargingContractTokenSymbol
            })).Balance;
            userBalance.ShouldBe(50);
        }

        private async Task TransferToken(ECKeyPair keyPair, string symbol, long amount)
        {
            var toAddress = Address.FromPublicKey(keyPair.PublicKey);
            if (symbol != EconomicSystemTestConstants.NativeTokenSymbol)
            {
                var buyResult = await TokenConverterContractStub.Buy.SendAsync(new BuyInput
                {
                    Symbol = symbol,
                    Amount = amount
                });
                buyResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            var transferResult = await TokenContractStub.Transfer.SendAsync(new TransferInput
            {
                To = toAddress,
                Symbol = symbol,
                Amount = amount,
                Memo = "transfer for test"
            });
            transferResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        private async Task<long> GetCurrentTreasuryBalance()
        {
            var balance = await TreasuryContractStub.GetUndistributedDividends.CallAsync(new Empty());

            return balance.Value[EconomicSystemTestConstants.NativeTokenSymbol];
        }
    }
}