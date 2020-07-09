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
        public async Task TransferAuthorizationForTokenConvert_Test()
        {
            var newParliament = new Parliament.CreateOrganizationInput
            {
                ProposerAuthorityRequired = false,
                ProposalReleaseThreshold = new Acs3.ProposalReleaseThreshold
                {
                    MaximalAbstentionThreshold = 1,
                    MaximalRejectionThreshold = 1,
                    MinimalApprovalThreshold = 1,
                    MinimalVoteThreshold = 1
                },
                ParliamentMemberProposingAllowed = false
            };
            var createNewParliament =
                (await ParliamentContractStub.CreateOrganization.SendAsync(newParliament)).TransactionResult;
            createNewParliament.Status.ShouldBe(TransactionResultStatus.Mined);
            var calculatedNewParliamentAddress = await ParliamentContractStub.CalculateOrganizationAddress.CallAsync(newParliament);
            var newAuthority = new Acs1.AuthorityInfo
            {
                ContractAddress = ParliamentContractAddress,
                OwnerAddress = calculatedNewParliamentAddress
            };
            await ExecuteProposalForParliamentTransaction(Tester, TokenConverterContractAddress, nameof(TokenConverterContractContainer.TokenConverterContractStub.ChangeConnectorController), newAuthority);
            var controller = await TokenConverterContractStub.GetControllerForManageConnector.CallAsync(new Empty());
            controller.OwnerAddress.ShouldBe(calculatedNewParliamentAddress);
        }

        [Fact]
        public async Task AddPairConnector_With_Invalid_Input_Test()
        {
            string tokenSymbol = "NETT";
            // add connector without authority
            {
                var pairConnector = GetLegalPairConnectorParam(tokenSymbol);
                var addConnectorWithoutAuthorityRet =
                    await TokenConverterContractStub.AddPairConnector.SendWithExceptionAsync(pairConnector);
                addConnectorWithoutAuthorityRet.TransactionResult.Error.ShouldContain(
                    "Only manager can perform this action.");
            }

            //input invalid token symbol
            {
                var pairConnector = GetLegalPairConnectorParam(tokenSymbol);
                pairConnector.ResourceConnectorSymbol = string.Empty;
                await ExecuteProposalForParliamentTransaction(Tester, TokenConverterContractAddress,
                    nameof(TokenConverterContractContainer.TokenConverterContractStub.AddPairConnector), pairConnector);
            }
            
        }
        
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
        
        [Fact]
        public async Task SetFeeRate_Success_Test()
        {
            //perform by non manager
            {
                var transactionResult = (await TokenConverterContractStub.SetFeeRate.SendAsync(
                    new StringValue
                    {
                        Value = "test value"
                    })).TransactionResult;
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Only manager can perform this action").ShouldBeTrue();
            }
            
            //invalid feeRate
            {
                var newRate = new StringValue
                {
                    Value = "test value"
                };
                var transactionResult = await ExecuteProposalForParliamentTransaction(Tester, TokenConverterContractAddress,
                    nameof(TokenConverterContractContainer.TokenConverterContractStub.SetFeeRate), newRate);
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Invalid decimal").ShouldBeTrue();
            }
            
            //feeRate not correct
            {
                var newRate = new StringValue
                {
                    Value = "1.05"
                };
                var transactionResult = await ExecuteProposalForParliamentTransaction(Tester, TokenConverterContractAddress,
                    nameof(TokenConverterContractContainer.TokenConverterContractStub.SetFeeRate), newRate);
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Fee rate has to be a decimal between 0 and 1").ShouldBeTrue();
            }
            
            //correct 
            {
                var newRate = new StringValue
                {
                    Value = "0.15"
                };
                var transactionResult = await ExecuteProposalForParliamentTransaction(Tester, TokenConverterContractAddress,
                    nameof(TokenConverterContractContainer.TokenConverterContractStub.SetFeeRate), newRate);
                transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
                var feeRate1 = await TokenConverterContractStub.GetFeeRate.CallAsync(new Empty());
                feeRate1.ShouldBe(newRate);
            }
        }
        
        [Fact]
        public async Task Set_Connector_Test()
        {
            var tokenSymbol = "TRA";
            //with authority user
            {
                var createTokenRet = (await TokenContractStub.Create.SendAsync(new CreateInput
                {
                    Symbol = tokenSymbol,
                    TokenName = "NET name",
                    TotalSupply = 100_0000_0000,
                    Issuer = BootMinerAddress,
                    IsBurnable = true
                })).TransactionResult;
                createTokenRet.Status.ShouldBe(TransactionResultStatus.Mined);
                var pairConnector = new PairConnectorParam
                {
                    ResourceConnectorSymbol = tokenSymbol,
                    ResourceWeight = "0.05",
                    NativeWeight = "0.05",
                    NativeVirtualBalance = 1_000_000_00000000,
                };
                await ExecuteProposalForParliamentTransaction(Tester, TokenConverterContractAddress,
                    nameof(TokenConverterContractContainer.TokenConverterContractStub.AddPairConnector), pairConnector);
                var ramNewInfo = (await TokenConverterContractStub.GetPairConnector.CallAsync(new TokenSymbol()
                {
                    Symbol = tokenSymbol
                })).ResourceConnector;
                ramNewInfo.IsPurchaseEnabled.ShouldBeFalse();
            }
        }
        
        [Fact]
        public async Task Update_Connector_Success_Test()
        {
            var token = "NETT";
            var createTokenRet = (await TokenContractStub.Create.SendAsync(new CreateInput
            {
                Symbol = token,
                TokenName = "NETT name",
                TotalSupply = 100_0000_0000,
                Issuer = BootMinerAddress,
                IsBurnable = true,
                LockWhiteList = { TokenConverterContractAddress}
            })).TransactionResult;
            createTokenRet.Status.ShouldBe(TransactionResultStatus.Mined);
            var pairConnector = new PairConnectorParam
            {
                ResourceConnectorSymbol = token,
                ResourceWeight = "0.05",
                NativeWeight = "0.05",
                NativeVirtualBalance = 1_0000_0000,
            };
            await ExecuteProposalForParliamentTransaction(Tester, TokenConverterContractAddress,
                nameof(TokenConverterContractContainer.TokenConverterContractStub.AddPairConnector), pairConnector);
            var updateConnector = new Connector
            {
                Symbol = token,
                VirtualBalance = 1000_000,
                IsVirtualBalanceEnabled = false,
                IsPurchaseEnabled = true,
                Weight = "0.49",
                RelatedSymbol = "change"
            };
            await ExecuteProposalForParliamentTransaction(Tester, TokenConverterContractAddress,
                nameof(TokenConverterContractContainer.TokenConverterContractStub.UpdateConnector), updateConnector);
            var resourceConnector = (await TokenConverterContractStub.GetPairConnector.CallAsync(new TokenSymbol {Symbol = token})).ResourceConnector;
            resourceConnector.Weight.ShouldBe("0.49");
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
    }
}