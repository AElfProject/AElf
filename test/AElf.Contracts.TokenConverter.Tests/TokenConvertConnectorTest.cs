using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.TokenConverter
{
    public partial class TokenConverterContractTests
    {
        public TokenConverterContractTests()
        {
            AsyncHelper.RunSync(InitializeParliamentContractAsync);
        }

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
            var calculatedNewParliamentAddress =
                await ParliamentContractStub.CalculateOrganizationAddress.CallAsync(newParliament);
            var newAuthority = new Acs1.AuthorityInfo
            {
                ContractAddress = ParliamentContractAddress,
                OwnerAddress = calculatedNewParliamentAddress
            };
            await ExecuteProposalForParliamentTransaction(TokenConverterContractAddress,
                nameof(TokenConverterContractContainer.TokenConverterContractStub.ChangeConnectorController),
                newAuthority);
            var controller = await DefaultStub
                .GetControllerForManageConnector.CallAsync(new Empty());
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
            if (tokenSymbol == "WRITE")
            {
                var writeConnector = GetLegalPairConnectorParam(tokenSymbol);
                await ExecuteProposalForParliamentTransaction(
                    TokenConverterContractAddress,
                    nameof(TokenConverterContractContainer.TokenConverterContractStub.AddPairConnector),
                    writeConnector);
            }

            var pairConnector = GetLegalPairConnectorParam(tokenSymbol);
            pairConnector.NativeWeight = nativeWeight;
            pairConnector.ResourceWeight = resourceWeight;
            var addPairConnectorRet = await ExecuteProposalForParliamentTransactionWithException(
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
                await DefaultStub.AddPairConnector.SendWithExceptionAsync(
                    pairConnector);
            addConnectorWithoutAuthorityRet.TransactionResult.Error.ShouldContain(
                "Only manager can perform this action.");
        }

        [Fact]
        public async Task AddPairConnector_Success_Test()
        {
            var tokenSymbol = "CWJ";
            var pairConnector = GetLegalPairConnectorParam(tokenSymbol);
            await ExecuteProposalForParliamentTransaction(
                TokenConverterContractAddress,
                nameof(TokenConverterContractContainer.TokenConverterContractStub.AddPairConnector), pairConnector);
            var getPairConnector =
                await DefaultStub.GetPairConnector.CallAsync(
                    new TokenSymbol
                    {
                        Symbol = tokenSymbol
                    });
            getPairConnector.ResourceConnector.Symbol.ShouldBe(tokenSymbol);
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
            var updateRet =
                await DefaultStub.UpdateConnector.SendWithExceptionAsync(
                    updateConnector);
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
            var pairConnector =
                await DefaultStub.GetPairConnector.CallAsync(
                    new TokenSymbol
                    {
                        Symbol = creatConnectorTokenSymbol
                    });
            var updateConnectorInput =
                isUpdateResourceToken ? pairConnector.ResourceConnector : pairConnector.DepositConnector;
            if (isUpdateResourceToken)
                updateConnectorInput.Symbol = inputTokenSymbol;
            updateConnectorInput.Weight = weight;
            updateConnectorInput.VirtualBalance = virtualBalance;

            if (!string.IsNullOrEmpty(error))
            {
                var updateRet = await ExecuteProposalForParliamentTransactionWithException(
                    TokenConverterContractAddress,
                    nameof(TokenConverterContractContainer.TokenConverterContractStub.UpdateConnector),
                    updateConnectorInput);
                updateRet.Error.ShouldContain(error);
            }
            else
            {
                await ExecuteProposalForParliamentTransaction(
                    TokenConverterContractAddress,
                    nameof(TokenConverterContractContainer.TokenConverterContractStub.UpdateConnector),
                    updateConnectorInput);
                var afterUpdatePairConnector =
                    await DefaultStub.GetPairConnector.CallAsync(
                        new TokenSymbol
                        {
                            Symbol = creatConnectorTokenSymbol
                        });
                var afterUpdateConnector =
                    isUpdateResourceToken
                        ? afterUpdatePairConnector.ResourceConnector
                        : afterUpdatePairConnector.DepositConnector;
                updateConnectorInput.Weight.ShouldBe(afterUpdateConnector.Weight);
                if (isUpdateResourceToken)
                    updateConnectorInput.VirtualBalance.ShouldNotBe(afterUpdateConnector.VirtualBalance);
                else
                    updateConnectorInput.VirtualBalance.ShouldBe(afterUpdateConnector.VirtualBalance);
            }
        }

        [Fact]
        public async Task Trade_With_UnPurchasable_Connector_Test()
        {
            var symbol = "PXY";
            await AddPairConnectorAsync(symbol);
            var buyRet = await DefaultStub.Buy.SendWithExceptionAsync(new BuyInput
            {
                Symbol = symbol,
                Amount = 100
            });
            buyRet.TransactionResult.Error.ShouldContain("can't purchase");

            var sellRet = await DefaultStub.Buy.SendWithExceptionAsync(new BuyInput
            {
                Symbol = symbol,
                Amount = 100
            });
            sellRet.TransactionResult.Error.ShouldContain("can't purchase");
        }

        [Fact]
        public async Task SetFeeRate_Test()
        {
            //not controller
            {
                var setFeeRateRet =
                    await DefaultStub.SetFeeRate.SendWithExceptionAsync(
                        new StringValue
                        {
                            Value = "0.5"
                        });
                setFeeRateRet.TransactionResult.Error.ShouldContain("Only manager can perform this action.");
            }

            var invalidRate = new StringValue();
            // can not parse
            {
                invalidRate.Value = "asd";
                var setFeeRateRet = await ExecuteProposalForParliamentTransactionWithException(
                    TokenConverterContractAddress,
                    nameof(TokenConverterContractContainer.TokenConverterContractStub.SetFeeRate), invalidRate);
                setFeeRateRet.Error.ShouldContain("Invalid decimal");
            }

            // == 1
            {
                invalidRate.Value = "1";
                var setFeeRateRet = await ExecuteProposalForParliamentTransactionWithException(
                    TokenConverterContractAddress,
                    nameof(TokenConverterContractContainer.TokenConverterContractStub.SetFeeRate), invalidRate);
                setFeeRateRet.Error.ShouldContain("Fee rate has to be a decimal between 0 and 1.");
            }

            // success
            {
                var validRate = new StringValue
                {
                    Value = "0.333"
                };
                await ExecuteProposalForParliamentTransaction(
                    TokenConverterContractAddress,
                    nameof(TokenConverterContractContainer.TokenConverterContractStub.SetFeeRate), validRate);
                var getFeeRate =
                    await DefaultStub.GetFeeRate.CallAsync(new Empty());
                getFeeRate.Value.ShouldBe(validRate.Value);
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
                    Issuer = DefaultSender,
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
                await ExecuteProposalForParliamentTransaction(TokenConverterContractAddress,
                    nameof(TokenConverterContractContainer.TokenConverterContractStub.AddPairConnector), pairConnector);
                var ramNewInfo =
                    (await DefaultStub.GetPairConnector.CallAsync(
                        new TokenSymbol()
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
                Issuer = DefaultSender,
                IsBurnable = true,
                LockWhiteList = {TokenConverterContractAddress}
            })).TransactionResult;
            createTokenRet.Status.ShouldBe(TransactionResultStatus.Mined);
            var pairConnector = new PairConnectorParam
            {
                ResourceConnectorSymbol = token,
                ResourceWeight = "0.05",
                NativeWeight = "0.05",
                NativeVirtualBalance = 1_0000_0000,
            };
            await ExecuteProposalForParliamentTransaction(TokenConverterContractAddress,
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
            await ExecuteProposalForParliamentTransaction(TokenConverterContractAddress,
                nameof(TokenConverterContractContainer.TokenConverterContractStub.UpdateConnector), updateConnector);
            var resourceConnector =
                (await DefaultStub.GetPairConnector.CallAsync(
                    new TokenSymbol {Symbol = token})).ResourceConnector;
            resourceConnector.Weight.ShouldBe("0.49");
        }
        
        [Fact] 
         public async Task Add_Pair_Connector_And_Enable_Success_Test()
         {
             await DefaultStub.Initialize.SendAsync(new InitializeInput
             {
                 FeeRate =  "0.005"
             });
            string tokenSymbol = "NETT";
            await CreateTokenAsync(tokenSymbol);
            var pairConnector = GetLegalPairConnectorParam(tokenSymbol);
            await ExecuteProposalForParliamentTransaction(TokenConverterContractAddress,
                nameof(TokenConverterContractContainer.TokenConverterContractStub.AddPairConnector), pairConnector);
            var resourceConnector = (await DefaultStub.GetPairConnector.CallAsync(new TokenSymbol {Symbol = tokenSymbol})).ResourceConnector;
            resourceConnector.ShouldNotBeNull();
            resourceConnector.IsPurchaseEnabled.ShouldBe(false);
            await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Amount = 99_9999_0000,
                To = DefaultSender,
                Symbol = tokenSymbol
            });
            var toBeBuildConnectorInfo = new ToBeConnectedTokenInfo
            {
                TokenSymbol = tokenSymbol,
                AmountToTokenConvert = 99_9999_0000
            }; 
            var deposit = await DefaultStub.GetNeededDeposit.CallAsync(toBeBuildConnectorInfo);
            deposit.NeedAmount.ShouldBe(100);
            var buildRet = (await DefaultStub.EnableConnector.SendAsync(toBeBuildConnectorInfo)).TransactionResult;
            buildRet.Status.ShouldBe(TransactionResultStatus.Mined);
            var tokenInTokenConvert = await GetBalanceAsync(tokenSymbol, TokenConverterContractAddress);
            tokenInTokenConvert.ShouldBe(99_9999_0000);
            resourceConnector = (await DefaultStub.GetPairConnector.CallAsync(new TokenSymbol {Symbol = tokenSymbol})).ResourceConnector;
            resourceConnector.ShouldNotBeNull();
            resourceConnector.IsPurchaseEnabled.ShouldBe(true);
            var beforeTokenBalance = await GetBalanceAsync(tokenSymbol, DefaultSender);
            var beforeBaseBalance = await GetBalanceAsync(NativeSymbol, DefaultSender);
            var buyRet = (await DefaultStub.Buy.SendAsync(new BuyInput
            {
                Symbol = tokenSymbol,
                Amount = 10000
            })).TransactionResult;
            buyRet.Status.ShouldBe(TransactionResultStatus.Mined);
            var afterTokenBalance = await GetBalanceAsync(tokenSymbol, DefaultSender);
            var afterBaseBalance = await GetBalanceAsync(NativeSymbol, DefaultSender);
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
                Issuer = DefaultSender,
                IsBurnable = true,
                LockWhiteList = {TokenConverterContractAddress}
            });
        }

        private async Task AddPairConnectorAsync(string tokenSymbol)
        {
            var pairConnector = GetLegalPairConnectorParam(tokenSymbol);
            await ExecuteProposalForParliamentTransaction(
                TokenConverterContractAddress,
                nameof(TokenConverterContractContainer.TokenConverterContractStub.AddPairConnector), pairConnector);
        }

        private async Task ApproveByParliamentMembers(Hash proposalId)
        {
            foreach (var bp in InitialCoreDataCenterKeyPairs)
            {
                var tester = GetParliamentContractTester(bp);
                await tester.Approve.SendAsync(proposalId);
            }
        }

        private async Task<Hash> CreateAndApproveProposalForParliament(Address contract,
            string method, IMessage input, Address parliamentOrganization = null)
        {
            if (parliamentOrganization == null)
                parliamentOrganization =
                    await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
            var proposal = new Acs3.CreateProposalInput
            {
                OrganizationAddress = parliamentOrganization,
                ContractMethodName = method,
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1),
                Params = input.ToByteString(),
                ToAddress = contract
            };
            var createResult = await ParliamentContractStub.CreateProposal.SendAsync(proposal);
            var proposalHash = createResult.Output;
            await ApproveByParliamentMembers(proposalHash);
            return proposalHash;
        }

        private async Task<TransactionResult> ExecuteProposalForParliamentTransactionWithException(
            Address contract,
            string method, IMessage input, Address parliamentOrganization = null)
        {
            if (parliamentOrganization == null)
                parliamentOrganization =
                    await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
            var proposalHash =
                await CreateAndApproveProposalForParliament(contract, method, input,
                    parliamentOrganization);
            var releaseResult = await ParliamentContractStub.Release.SendWithExceptionAsync(proposalHash);
            return releaseResult.TransactionResult;
        }

        private async Task ExecuteProposalForParliamentTransaction(Address contract,
            string method, IMessage input, Address parliamentOrganization = null)
        {
            if (parliamentOrganization == null)
                parliamentOrganization =
                    await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
            var proposalHash =
                await CreateAndApproveProposalForParliament(contract, method, input,
                    parliamentOrganization);
            await ParliamentContractStub.Release.SendAsync(proposalHash);
        }
    }
}