using System.Threading.Tasks;
using AElf.Contracts.Economic.TestBase;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TokenConverter;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.EconomicSystem.Tests.BVT
{
    public partial class EconomicSystemTest
    {
        [Fact] 
         public async Task Add_Pair_Connector_And_Enable_Success_Test()
        {
            string tokenSymbol = "NETT";
            await CreateTokenAsync(tokenSymbol);
            var pairConnector = GetLegalPairConnectorParam(tokenSymbol);
            await ExecuteProposalForParliamentTransaction(Tester, TokenConverterContractAddress,
                nameof(TokenConverterContractContainer.TokenConverterContractStub.AddPairConnector), pairConnector);
            var resourceConnector = (await TokenConverterContractStub.GetPairConnector.CallAsync(new TokenSymbol {Symbol = tokenSymbol})).ResourceConnector;
            resourceConnector.ShouldNotBeNull();
            resourceConnector.IsPurchaseEnabled.ShouldBe(false);
            await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Amount = 99_9999_0000,
                To = BootMinerAddress,
                Symbol = tokenSymbol
            });
            var toBeBuildConnectorInfo = new ToBeConnectedTokenInfo
            {
                TokenSymbol = tokenSymbol,
                AmountToTokenConvert = 99_9999_0000
            }; 
            var deposit = await TokenConverterContractStub.GetNeededDeposit.CallAsync(toBeBuildConnectorInfo);
            deposit.NeedAmount.ShouldBe(100);
            var buildRet = (await TokenConverterContractStub.EnableConnector.SendAsync(toBeBuildConnectorInfo)).TransactionResult;
            buildRet.Status.ShouldBe(TransactionResultStatus.Mined);
            var tokenInTokenConvert = await GetBalanceAsync(tokenSymbol, TokenConverterContractAddress);
            tokenInTokenConvert.ShouldBe(99_9999_0000);
            resourceConnector = (await TokenConverterContractStub.GetPairConnector.CallAsync(new TokenSymbol {Symbol = tokenSymbol})).ResourceConnector;
            resourceConnector.ShouldNotBeNull();
            resourceConnector.IsPurchaseEnabled.ShouldBe(true);
            var beforeTokenBalance = await GetBalanceAsync(tokenSymbol, BootMinerAddress);
            var beforeBaseBalance = await GetBalanceAsync(EconomicContractsTestConstants.NativeTokenSymbol, BootMinerAddress);
            var buyRet = (await TokenConverterContractStub.Buy.SendAsync(new BuyInput
            {
                Symbol = tokenSymbol,
                Amount = 10000
            })).TransactionResult;
            buyRet.Status.ShouldBe(TransactionResultStatus.Mined);
            var afterTokenBalance = await GetBalanceAsync(tokenSymbol, BootMinerAddress);
            var afterBaseBalance = await GetBalanceAsync(EconomicContractsTestConstants.NativeTokenSymbol, BootMinerAddress);
            (afterTokenBalance - beforeTokenBalance).ShouldBe(10000);
            (beforeBaseBalance - afterBaseBalance).ShouldBe(100);
        }
         
        private PairConnectorParam GetLegalPairConnectorParam(string tokenSymbol, long nativeBalance = 1_0000_0000,
            string resourceWeight = "0.05", string nativeWeight = "0.05")
        {
            return new PairConnectorParam
            {
                ResourceConnectorSymbol = tokenSymbol,
                ResourceWeight = resourceWeight,
                NativeWeight = nativeWeight,
                NativeVirtualBalance = nativeBalance
            };
        }

        private async Task CreateTokenAsync(string symbol, long totalSupply = 100_0000_0000)
        {
            await TokenContractStub.Create.SendAsync(new CreateInput
            {
                Symbol = symbol,
                TokenName = symbol + " name",
                TotalSupply = totalSupply,
                Issuer = BootMinerAddress,
                IsBurnable = true,
                LockWhiteList = {TokenConverterContractAddress}
            });
        }
        
        private async Task<long> GetBalanceAsync(string symbol, Address owner)
        {
            var balanceResult = await TokenContractStub.GetBalance.CallAsync(
                new GetBalanceInput
                {
                    Owner = owner,
                    Symbol = symbol
                });
            return balanceResult.Balance;
        }

        private async Task AddPairConnectorAsync(string tokenSymbol)
        {
            var pairConnector = GetLegalPairConnectorParam(tokenSymbol);
            await ExecuteProposalForParliamentTransaction(Tester,
                TokenConverterContractAddress,
                nameof(TokenConverterContractContainer.TokenConverterContractStub.AddPairConnector), pairConnector);
        }
    }
}