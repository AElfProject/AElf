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


        [Theory]
        [InlineData("WRITE", "0.5", "0.5", "resource token symbol has existed")]
        [InlineData("", "0.5", "0.5", "resource token symbol should not be empty")]
        [InlineData("N89", "0.2", "0.5", "Invalid symbol.")]
        [InlineData("MKA", "0", "0.5", "Connector Shares has to be a decimal between 0 and 1.")]
        [InlineData("JUN", "0.9", "1", "Connector Shares has to be a decimal between 0 and 1.")]
        public async Task AddPairConnector_With_Invalid_Input_Test(string tokenSymbol, string resourceWeight,
            string nativeWeight, string errorMessage)
        {
            var pairConnector = GetLegalPairConnectorParam(tokenSymbol);
            pairConnector.NativeWeight = nativeWeight;
            pairConnector.ResourceWeight = resourceWeight;
            var addPairConnectorRet = await ExecuteProposalForParliamentTransactionWithoutCheck(Tester,
                TokenConverterContractAddress,
                nameof(TokenConverterContractContainer.TokenConverterContractStub.AddPairConnector), pairConnector);
            addPairConnectorRet.Error.ShouldContain(errorMessage);
        }
        
        [Fact]
        public async Task AddPairConnector_Without_Authority_Test()
        {
            string tokenSymbol = "NETT";
            var pairConnector = GetLegalPairConnectorParam(tokenSymbol);
            var addConnectorWithoutAuthorityRet =
                await TokenConverterContractStub.AddPairConnector.SendAsync(pairConnector);
            addConnectorWithoutAuthorityRet.TransactionResult.Error.ShouldContain(
                "Only manager can perform this action.");
        }
        
        [Fact]
        public async Task AddPairConnector_Success_Test()
        {
            var tokenSymbol = "CWJ";
            var pairConnector = GetLegalPairConnectorParam(tokenSymbol);
            await ExecuteProposalForParliamentTransaction(Tester,
                TokenConverterContractAddress,
                nameof(TokenConverterContractContainer.TokenConverterContractStub.AddPairConnector), pairConnector);
            var getPairConnector = await TokenConverterContractStub.GetPairConnector.CallAsync(new TokenSymbol
            {
                Symbol = tokenSymbol
            });
            getPairConnector.ResourceConnector.Symbol.Equals(tokenSymbol).ShouldBeTrue();
        }

        [Fact]
        public async Task UpdateConnector_Without_Authority_Test()
        {
            var tokenSymbol = "CWJ";
            await AddPairConnectorAsync(tokenSymbol);
            var updateConnector = new Connector
            {
                Symbol = tokenSymbol,
                Weight = "0.3"
            };
            var updateRet = await TokenConverterContractStub.UpdateConnector.SendAsync(updateConnector);
            updateRet.TransactionResult.Error.ShouldContain("Only manager can perform this action.");
        }
        
        // if update resource token connector, the virtual balance as a parameter should not equal to the origin one.
        [Theory]
        [InlineData(true, "", "0.5", 0, "input symbol can not be empty")]
        [InlineData(true, "TTA", "0.5", 0, "Can not find target connector.")]
        [InlineData(true, "LIO", "0.5a", 0, "Invalid decimal")]
        [InlineData(true, "LIO", "1", 0, "Connector Shares has to be a decimal between 0 and 1.")]
        [InlineData(true, "LIO", "0.5", 20, "")]
        [InlineData(false, "LIO", "0.3", 200, "")]
        public async Task UpdateConnector_Test(bool isUpdateResourceToken, string inputTokenSymbol,
            string weight, long virtualBalance, string error)
        {
            var creatConnectorTokenSymbol = "LIO";
            await AddPairConnectorAsync(creatConnectorTokenSymbol);
            var pairConnector = await TokenConverterContractStub.GetPairConnector.CallAsync(new TokenSymbol
            {
                Symbol = creatConnectorTokenSymbol
            });
            var updateConnectorInput =
                isUpdateResourceToken ? pairConnector.ResourceConnector : pairConnector.ResourceConnector;
            if(isUpdateResourceToken)
                updateConnectorInput.Symbol = inputTokenSymbol;
            updateConnectorInput.Weight = weight;
            updateConnectorInput.VirtualBalance = virtualBalance;
            var updateRet = await ExecuteProposalForParliamentTransactionWithoutCheck(Tester,
                TokenConverterContractAddress,
                nameof(TokenConverterContractContainer.TokenConverterContractStub.UpdateConnector), updateConnectorInput);
            if(!string.IsNullOrEmpty(error))
                updateRet.Error.ShouldContain(error);
            else
            {
                updateRet.Status.ShouldBe(TransactionResultStatus.Mined);
                var afterUpdatePairConnector = await TokenConverterContractStub.GetPairConnector.CallAsync(new TokenSymbol
                {
                    Symbol = creatConnectorTokenSymbol
                });
                var afterUpdateConnector =
                    isUpdateResourceToken ? afterUpdatePairConnector.ResourceConnector : pairConnector.ResourceConnector;
                updateConnectorInput.Weight.Equals(afterUpdateConnector.Weight).ShouldBeTrue();
                if(isUpdateResourceToken)
                    updateConnectorInput.VirtualBalance.ShouldNotBe(afterUpdateConnector.VirtualBalance);
                else
                    updateConnectorInput.VirtualBalance.ShouldBe(afterUpdateConnector.VirtualBalance);
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
                var transactionResult = await ExecuteProposalForParliamentTransactionWithoutCheck(Tester, TokenConverterContractAddress,
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
                var transactionResult = await ExecuteProposalForParliamentTransactionWithoutCheck(Tester, TokenConverterContractAddress,
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
                await ExecuteProposalForParliamentTransaction(Tester, TokenConverterContractAddress,
                    nameof(TokenConverterContractContainer.TokenConverterContractStub.SetFeeRate), newRate);

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

        private async Task AddPairConnectorAsync(string tokenSymbol)
        {
            var pairConnector = GetLegalPairConnectorParam(tokenSymbol);
            await ExecuteProposalForParliamentTransaction(Tester,
                TokenConverterContractAddress,
                nameof(TokenConverterContractContainer.TokenConverterContractStub.AddPairConnector), pairConnector);
        }
    }
}