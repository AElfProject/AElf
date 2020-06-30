using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs1;
using Acs3;
using AElf.Contracts.Association;
using AElf.Contracts.Genesis;
using AElf.Contracts.Parliament;
using AElf.Contracts.Referendum;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;
using CreateOrganizationInput = AElf.Contracts.Parliament.CreateOrganizationInput;

namespace AElf.Contracts.MultiToken
{
    public partial class MultiTokenAuthorizedTest : MultiTokenContractCrossChainTestBase
    {
        public MultiTokenAuthorizedTest()
        {
            AsyncHelper.RunSync(InitializeTokenContract);
        }

        private async Task InitializeTokenContract()
        {
            var initControllerResult = await MainChainTester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub.InitializeAuthorizedController), new Empty());
            initControllerResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task Controller_Transfer_For_Symbol_To_Pay_Tx_Fee()
        {
            var primaryTokenRet = await MainChainTester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub.GetPrimaryTokenSymbol), new Empty());
            var primarySymbol = new StringValue();
            primarySymbol.MergeFrom(primaryTokenRet.ReturnValue);
            var newSymbolList = new SymbolListToPayTxSizeFee();
            newSymbolList.SymbolsToPayTxSizeFee.Add(new SymbolToPayTxSizeFee
            {
                TokenSymbol = primarySymbol.Value,
                AddedTokenWeight = 1,
                BaseTokenWeight = 1
            });
            
            // create a new organization to be replace the controller of SetSymbolsToPayTxSizeFee
            var newParliament = new CreateOrganizationInput
            {
                ProposerAuthorityRequired = false,
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MaximalAbstentionThreshold = 1,
                    MaximalRejectionThreshold = 1,
                    MinimalApprovalThreshold = 1,
                    MinimalVoteThreshold = 1
                },
                ParliamentMemberProposingAllowed = false
            };
            var parliamentCreateRet = await MainChainTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.CreateOrganization), newParliament);
            var newParliamentAddress = new Address();
            newParliamentAddress.MergeFrom(parliamentCreateRet.ReturnValue);
            var newAuthority = new AuthorityInfo
            {
                ContractAddress = ParliamentAddress,
                OwnerAddress = newParliamentAddress
            };
            
            // get the default parliament that is the controller of SetSymbolsToPayTxSizeFee
            var defaultParliamentAddress = await GetDefaultParliamentAddress();
            
            // create a proposal to replace the controller
            var createProposalInput = new CreateProposalInput
            {
                ToAddress = TokenContractAddress,
                Params = newAuthority.ToByteString(),
                OrganizationAddress = defaultParliamentAddress,
                ContractMethodName = nameof(TokenContractImplContainer.TokenContractImplStub
                    .ChangeSymbolsToPayTXSizeFeeController),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            await MainChainTesterCreatApproveAndReleaseProposalForParliament(createProposalInput);
            
            // the new controller try to send SetSymbolsToPayTxSizeFee
            var updateInput = new CreateProposalInput
            {
                ToAddress = TokenContractAddress,
                Params = newSymbolList.ToByteString(),
                OrganizationAddress = newParliamentAddress,
                ContractMethodName = nameof(TokenContractImplContainer.TokenContractImplStub.SetSymbolsToPayTxSizeFee),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            await MainChainTesterCreatApproveAndReleaseProposalForParliament(updateInput);
            
            //verify the result that the symbol list is updated
            var symbolSetRet = await MainChainTester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub.GetSymbolsToPayTxSizeFee), newSymbolList);
            symbolSetRet.Status.ShouldBe(TransactionResultStatus.Mined);
            var updatedSymbolList = new SymbolListToPayTxSizeFee();
            updatedSymbolList.MergeFrom(symbolSetRet.ReturnValue);
            updatedSymbolList.SymbolsToPayTxSizeFee.Count.ShouldBe(1);
        }

        //fee type : read = 0, storage = 1, write =2, traffic = 3
        [Theory]
        [InlineData(false, 3, new []{1}, new [] {1000000, 4, 3, 2})]
        [InlineData(false, 0, new []{2}, new []{999999, 1, 4, 2, 5, 250, 40})]
        [InlineData(false, 0, new []{3}, new []{int.MaxValue, 2, 8, 2, 6, 300, 50})]
        [InlineData(false, 2, new []{2,3}, new []{100, 1, 4, 10000},new []{1000000, 1, 4, 2, 2, 250, 50})]
        [InlineData(true, 0, new []{2}, new []{int.MaxValue, 4, 3, 2})]
        [InlineData(true, 0, new []{3}, new []{int.MaxValue, 2, 8, 2, 6, 300})]
        [InlineData(true, 0, new []{3,2}, new []{1000, 4, 3, 2}, new []{int.MaxValue, 4, 3, 2})]
        [InlineData(true, 0, new []{2,3}, new []{100, 4, 3, 2})]
        public async Task Update_Coefficient_For_Contract_Test(bool isFail, int feeType, int[] pieceNumber, 
            params int[][] newPieceFunctions)
        {
            var originalCoefficients = await GetCalculateFeeCoefficientsByFeeType(feeType);
            var newPieceCoefficientList = newPieceFunctions.Select(x => new CalculateFeePieceCoefficients
            {
                Value = {x}
            }).ToList();
            var updateInput = new UpdateCoefficientsInput
            {
                PieceNumbers = {pieceNumber},
                Coefficients = new CalculateFeeCoefficients
                {
                    FeeTokenType = feeType,
                }
            };
            updateInput.Coefficients.PieceCoefficientsList.AddRange(newPieceCoefficientList);
            var proposalId = await CreateToRootForDeveloperFeeByTwoLayer(updateInput);
            await ApproveToRootForDeveloperFeeByTwoLayer(proposalId);
            var middleApproveProposalId = await ApproveToRootForDeveloperFeeByMiddleLayer(proposalId);
            await ApproveThenReleaseMiddleProposalForDeveloper(middleApproveProposalId);
            await ReleaseToRootForDeveloperFeeByTwoLayer(proposalId);
            var updatedCoefficients = await GetCalculateFeeCoefficientsByFeeType(feeType);
            if (!isFail)
            {
                foreach (var newPieceFunction in newPieceFunctions)
                {
                    var hasModified =
                        GetCalculateFeePieceCoefficients(updatedCoefficients.PieceCoefficientsList, newPieceFunction[0]);
                    var newCoefficient = newPieceFunction.Skip(1).ToArray();
                    hasModified.Value.Skip(1).ShouldBe(newCoefficient);
                }
            }
            else
            {
                var pieceCount = originalCoefficients.PieceCoefficientsList.Count;
                updatedCoefficients.PieceCoefficientsList.Count.ShouldBe(pieceCount);
                for (var i = 0; i < pieceCount; i++)
                {
                    originalCoefficients.PieceCoefficientsList[i]
                        .ShouldBe(updatedCoefficients.PieceCoefficientsList[i]);
                }
            }
        }
        
                //fee type : read = 0, storage = 1, write =2, traffic = 3
        [Theory]
        [InlineData(false, new []{1}, new [] {100, 4, 3, 2})]
        [InlineData(false, new []{2,3}, new []{5000001, 1, 4, 10000},new []{5000002, 1, 4, 2, 2, 250, 50})]
        [InlineData(true, new []{2}, new []{int.MaxValue, 4, 3, 2})]
        [InlineData(true, new []{1}, new [] {100, 4, 3})]
        [InlineData(true, new []{2,3}, new []{5000001, 1, 4, 10000},new []{5000001, 1, 4, 2, 2, 250, 50})]
        [InlineData(true, new []{3,2}, new []{5000001, 1, 4, 10000},new []{5000002, 1, 4, 2, 2, 250, 50})]
        [InlineData(true, new []{2,3}, new []{5000002, 4, 3, 2})]
        public async Task Update_Coefficient_For_Sender_Test(bool isFail, int[] pieceNumber, 
            params int[][] newPieceFunctions)
        {
            var feeType = (int) FeeTypeEnum.Tx;
            var primaryTokenSymbol = await GetThePrimaryToken();
            await IssuePrimaryTokenToMainChainTester();
            var originalCoefficients = await GetCalculateFeeCoefficientsByFeeType(feeType);
            var newPieceCoefficientList = newPieceFunctions.Select(x => new CalculateFeePieceCoefficients
            {
                Value = {x}
            }).ToList();
            var updateInput = new UpdateCoefficientsInput
            {
                PieceNumbers = {pieceNumber},
                Coefficients = new CalculateFeeCoefficients
                {
                    FeeTokenType = feeType,
                }
            };
            updateInput.Coefficients.PieceCoefficientsList.AddRange(newPieceCoefficientList);
            
            var proposalId = await CreateToRootForUserFeeByTwoLayer(updateInput);
            await ApproveToRootForUserFeeByTwoLayer(proposalId);
            await VoteToReferendum(proposalId, primaryTokenSymbol);
            await ReleaseToRootForUserFeeByTwoLayer(proposalId);
            
            var updatedCoefficients = await GetCalculateFeeCoefficientsByFeeType(feeType);
            if (!isFail)
            {
                foreach (var newPieceFunction in newPieceFunctions)
                {
                    var hasModified =
                        GetCalculateFeePieceCoefficients(updatedCoefficients.PieceCoefficientsList, newPieceFunction[0]);
                    var newCoefficient = newPieceFunction.Skip(1).ToArray();
                    hasModified.Value.Skip(1).ShouldBe(newCoefficient);
                }
            }
            else
            {
                var pieceCount = originalCoefficients.PieceCoefficientsList.Count;
                updatedCoefficients.PieceCoefficientsList.Count.ShouldBe(pieceCount);
                for (var i = 0; i < pieceCount; i++)
                {
                    originalCoefficients.PieceCoefficientsList[i]
                        .ShouldBe(updatedCoefficients.PieceCoefficientsList[i]);
                }
            }
        }
        
        private async Task<CalculateFeeCoefficients> GetCalculateFeeCoefficientsByFeeType(int feeType)
        {
            if (feeType == (int) FeeTypeEnum.Tx)
            {
                var userCoefficientRet = await MainChainTester.ExecuteContractWithMiningAsync(TokenContractAddress,
                    nameof(TokenContractImplContainer.TokenContractImplStub.GetCalculateFeeCoefficientsForSender),
                    new Empty());
                userCoefficientRet.Status.ShouldBe(TransactionResultStatus.Mined);
                var userCoefficient = new CalculateFeeCoefficients();
                userCoefficient.MergeFrom(userCoefficientRet.ReturnValue);
                return userCoefficient;
            }
            var developerCoefficientRet = await MainChainTester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub.GetCalculateFeeCoefficientsForContract),
                new Int32Value
                {
                    Value = feeType
                });
            developerCoefficientRet.Status.ShouldBe(TransactionResultStatus.Mined);
            var developerCoefficient = new CalculateFeeCoefficients();
            developerCoefficient.MergeFrom(developerCoefficientRet.ReturnValue);
            return developerCoefficient;
        }

        private CalculateFeePieceCoefficients GetCalculateFeePieceCoefficients(
            IEnumerable<CalculateFeePieceCoefficients> calculateFeePieceCoefficients, int pieceUpperBound)
        {
            return calculateFeePieceCoefficients.SingleOrDefault(c => c.Value[0] == pieceUpperBound);
        }

        [Fact]
        public async Task MethodFeeController_Test()
        {
            var byteResult = await MainChainTester.CallContractMethodAsync(TokenContractAddress,
                nameof(BasicContractZeroContainer.BasicContractZeroBase.GetMethodFeeController),
                new Empty());
            var defaultController = AuthorityInfo.Parser.ParseFrom(byteResult);

            var createOrganizationResult =
                await MainChainTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                    nameof(ParliamentContractContainer.ParliamentContractStub.CreateOrganization),
                    new CreateOrganizationInput
                    {
                        ProposalReleaseThreshold = new ProposalReleaseThreshold
                        {
                            MinimalApprovalThreshold = 1000,
                            MinimalVoteThreshold = 1000
                        }
                    });
            var organizationAddress = Address.Parser.ParseFrom(createOrganizationResult.ReturnValue);

            //change controller
            var newController = new AuthorityInfo
            {
                ContractAddress = defaultController.ContractAddress,
                OwnerAddress = organizationAddress
            };
            var createProposalInput = new CreateProposalInput
            {
                ToAddress = TokenContractAddress,
                Params = newController.ToByteString(),
                OrganizationAddress = defaultController.OwnerAddress,
                ContractMethodName = nameof(BasicContractZeroContainer.BasicContractZeroBase.ChangeMethodFeeController),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            await MainChainTesterCreatApproveAndReleaseProposalForParliament(createProposalInput);

            byteResult = await MainChainTester.CallContractMethodAsync(TokenContractAddress,
                nameof(BasicContractZeroContainer.BasicContractZeroBase.GetMethodFeeController),
                new Empty());
            var queryController = AuthorityInfo.Parser.ParseFrom(byteResult);
            queryController.ShouldBe(newController);
        }

        [Fact]
        public async Task Change_CrossChainTokenContract_RegistrationController_Test()
        {
            var createOrganizationResult = await MainChainTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.CreateOrganization),
                new CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MaximalAbstentionThreshold = 1,
                        MaximalRejectionThreshold = 1,
                        MinimalApprovalThreshold = 1,
                        MinimalVoteThreshold = 1
                    }
                });

            var newOrganization = Address.Parser.ParseFrom(createOrganizationResult.ReturnValue);

            var transactionResult = await MainChainTester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub
                    .GetCrossChainTokenContractRegistrationController),
                new Empty());
            var defaultController = AuthorityInfo.Parser.ParseFrom(transactionResult.ReturnValue);
            var transactionResult2 = await MainChainTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.GetDefaultOrganizationAddress),
                new Empty());
            var defaultOrganization = Address.Parser.ParseFrom(transactionResult2.ReturnValue);
            defaultController.OwnerAddress.ShouldBe(defaultOrganization);

            const string proposalCreationMethodName = nameof(TokenContractImplContainer.TokenContractImplStub
                .ChangeCrossChainTokenContractRegistrationController);
            var newAuthority = new AuthorityInfo
            {
                ContractAddress = ParliamentAddress,
                OwnerAddress = newOrganization
            };
            var proposalId = await CreateProposalAsync(MainChainTester, ParliamentAddress,
                proposalCreationMethodName, newAuthority.ToByteString(),
                TokenContractAddress);

            await ApproveWithMinersAsync(proposalId, ParliamentAddress, MainChainTester);
            var txResult = await ReleaseProposalAsync(proposalId, ParliamentAddress, MainChainTester);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var txResult2 = await MainChainTester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub
                    .GetCrossChainTokenContractRegistrationController),
                new Empty());
            ;
            var newController = AuthorityInfo.Parser.ParseFrom(txResult2.ReturnValue);
            Assert.True(newController.OwnerAddress == newOrganization);
        }

        [Fact]
        public async Task Change_CrossChainTokenContract_RegistrationController_WithoutAuth_Test()
        {
            var createOrganizationResult = await MainChainTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.CreateOrganization),
                new CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MaximalAbstentionThreshold = 1,
                        MaximalRejectionThreshold = 1,
                        MinimalApprovalThreshold = 1,
                        MinimalVoteThreshold = 1
                    }
                });

            var newOrganization = Address.Parser.ParseFrom(createOrganizationResult.ReturnValue);
            var newAuthority = new AuthorityInfo
            {
                ContractAddress = ParliamentAddress,
                OwnerAddress = newOrganization
            };
            var result = await MainChainTester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub
                    .ChangeCrossChainTokenContractRegistrationController),
                newAuthority);
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("No permission.").ShouldBeTrue();
        }

        private async Task<TransactionResult> MainChainTesterCreatApproveAndReleaseProposalForParliament(CreateProposalInput proposalInput)
        {
            var parliamentCreateProposal = await MainChainTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.CreateProposal),
                proposalInput);
            parliamentCreateProposal.Status.ShouldBe(TransactionResultStatus.Mined);
            var parliamentProposalId = new Hash();
            parliamentProposalId.MergeFrom(parliamentCreateProposal.ReturnValue);
            await ApproveWithMinersAsync(parliamentProposalId, ParliamentAddress, MainChainTester);
            var releaseRet = await ReleaseProposalAsync(parliamentProposalId, ParliamentAddress, MainChainTester);
            //releaseRet.Status.ShouldBe(TransactionResultStatus.Mined);
            return releaseRet;
        }

        private async Task<Hash> CreateToRootForDeveloperFeeByTwoLayer(UpdateCoefficientsInput input)
        {
            var organizations = await GetControllerForDeveloperFee();
            var createNestProposalInput = new CreateProposalInput
            {
                ToAddress = TokenContractAddress,
                Params = input.ToByteString(),
                OrganizationAddress = organizations.RootController.OwnerAddress,
                ContractMethodName =
                    nameof(TokenContractImplContainer.TokenContractImplStub.UpdateCoefficientsForContract),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            var createProposalInput = new CreateProposalInput
            {
                ToAddress = AssociationAddress,
                Params = createNestProposalInput.ToByteString(),
                OrganizationAddress = organizations.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.CreateProposal),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };

            var releaseRet = await MainChainTesterCreatApproveAndReleaseProposalForParliament(createProposalInput);
            var id = ProposalCreated.Parser
                .ParseFrom(releaseRet.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                    .NonIndexed).ProposalId;
            return id;
        }

        private async Task ApproveToRootForDeveloperFeeByTwoLayer(Hash input)
        {
            var organizations = await GetControllerForDeveloperFee();
            var approveProposalInput = new CreateProposalInput
            {
                ToAddress = AssociationAddress,
                Params = input.ToByteString(),
                OrganizationAddress = organizations.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.Approve),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            await MainChainTesterCreatApproveAndReleaseProposalForParliament(approveProposalInput);
        }

        private async Task ReleaseToRootForDeveloperFeeByTwoLayer(Hash input)
        {
            var organizations = await GetControllerForDeveloperFee();
            var releaseProposalInput = new CreateProposalInput
            {
                ToAddress = AssociationAddress,
                Params = input.ToByteString(),
                OrganizationAddress = organizations.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.Release),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            await MainChainTesterCreatApproveAndReleaseProposalForParliament(releaseProposalInput);
        }

        private async Task<Hash> ApproveToRootForDeveloperFeeByMiddleLayer(Hash input)
        {
            var organizations = await GetControllerForDeveloperFee();
            var approveMidProposalInput = new CreateProposalInput
            {
                ToAddress = AssociationAddress,
                Params = input.ToByteString(),
                OrganizationAddress = organizations.DeveloperController.OwnerAddress,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.Approve),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            var approveLeafProposalInput = new CreateProposalInput
            {
                ToAddress = AssociationAddress,
                Params = approveMidProposalInput.ToByteString(),
                OrganizationAddress = organizations.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.CreateProposal),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            var newCreateProposalRet =
                await MainChainTesterCreatApproveAndReleaseProposalForParliament(approveLeafProposalInput);
            var middleProposalId = ProposalCreated.Parser
                .ParseFrom(newCreateProposalRet.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                    .NonIndexed).ProposalId;
            return middleProposalId;
        }

        private async Task ApproveThenReleaseMiddleProposalForDeveloper(Hash input)
        {
            var organizations = await GetControllerForDeveloperFee();
            var approveLeafProposalInput = new CreateProposalInput
            {
                ToAddress = AssociationAddress,
                Params = input.ToByteString(),
                OrganizationAddress = organizations.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.Approve),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            await MainChainTesterCreatApproveAndReleaseProposalForParliament(approveLeafProposalInput);
            
            approveLeafProposalInput = new CreateProposalInput
            {
                ToAddress = AssociationAddress,
                Params = input.ToByteString(),
                OrganizationAddress = organizations.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.Release),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            await MainChainTesterCreatApproveAndReleaseProposalForParliament(approveLeafProposalInput);
        }

        private async Task<Hash> CreateToRootForUserFeeByTwoLayer(UpdateCoefficientsInput input)
        {
            var organizations = await GetControllerForUserFee();
            var createNestProposalInput = new CreateProposalInput
            {
                ToAddress = TokenContractAddress,
                Params = input.ToByteString(),
                OrganizationAddress = organizations.RootController.OwnerAddress,
                ContractMethodName =
                    nameof(TokenContractImplContainer.TokenContractImplStub.UpdateCoefficientsForSender),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };

            var createProposalInput = new CreateProposalInput
            {
                ToAddress = AssociationAddress,
                Params = createNestProposalInput.ToByteString(),
                OrganizationAddress = organizations.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.CreateProposal),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            var releaseRet = await MainChainTesterCreatApproveAndReleaseProposalForParliament(createProposalInput);

            var id = ProposalCreated.Parser
                .ParseFrom(releaseRet.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                    .NonIndexed).ProposalId;
            return id;
        }

        private async Task ApproveToRootForUserFeeByTwoLayer(Hash input)
        {
            var organizations = await GetControllerForUserFee();
            var approveProposalInput = new CreateProposalInput
            {
                ToAddress = AssociationAddress,
                Params = input.ToByteString(),
                OrganizationAddress = organizations.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.Approve),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            await MainChainTesterCreatApproveAndReleaseProposalForParliament(approveProposalInput);
        }

        private async Task VoteToReferendum(Hash input, string primaryTokenSymbol)
        {
            var organizations = await GetControllerForUserFee();

            var referendumProposal = new CreateProposalInput
            {
                ToAddress = AssociationAddress,
                Params = input.ToByteString(),
                OrganizationAddress = organizations.ReferendumController.OwnerAddress,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.Approve),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            
            var parliamentProposal = new CreateProposalInput
            {
                ToAddress = ReferendumAddress,
                Params = referendumProposal.ToByteString(),
                OrganizationAddress = organizations.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(ReferendumContractContainer.ReferendumContractStub.CreateProposal),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            var ret = await MainChainTesterCreatApproveAndReleaseProposalForParliament(parliamentProposal);
            var referendumProposalId = ProposalCreated.Parser
                .ParseFrom(ret.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                    .NonIndexed).ProposalId;
            var proposalVirtualAddress = await MainChainTester.ExecuteContractWithMiningAsync(ReferendumAddress,
                nameof(ReferendumContractContainer.ReferendumContractStub.GetProposalVirtualAddress), referendumProposalId);
            var approveResult = await MainChainTester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub.Approve), new ApproveInput
                {
                    Spender = Address.Parser.ParseFrom(proposalVirtualAddress.ReturnValue),
                    Symbol = primaryTokenSymbol,
                    Amount = 100000
                });
            approveResult.Status.ShouldBe(TransactionResultStatus.Mined);
            await MainChainTester.ExecuteContractWithMiningAsync(ReferendumAddress,
                nameof(ReferendumContractContainer.ReferendumContractStub.Approve),
                referendumProposalId);

            parliamentProposal = new CreateProposalInput
            {
                ToAddress = ReferendumAddress,
                Params = referendumProposalId.ToByteString(),
                OrganizationAddress = organizations.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(ReferendumContractContainer.ReferendumContractStub.Release),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            await MainChainTesterCreatApproveAndReleaseProposalForParliament(parliamentProposal);
        }

        private async Task ReleaseToRootForUserFeeByTwoLayer(Hash input)
        {
            var organizations = await GetControllerForUserFee();
            var parliamentProposal = new CreateProposalInput
            {
                ToAddress = AssociationAddress,
                Params = input.ToByteString(),
                OrganizationAddress = organizations.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.Release),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            await MainChainTesterCreatApproveAndReleaseProposalForParliament(parliamentProposal);
        }

        private async Task<UserFeeController> GetControllerForUserFee()
        {
            var organizationInfoRet = await MainChainTester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub.GetUserFeeController), new Empty());
            organizationInfoRet.Status.ShouldBe(TransactionResultStatus.Mined);
            var organizationInfo = new UserFeeController();
            organizationInfo.MergeFrom(organizationInfoRet.ReturnValue);
            return organizationInfo;
        }

        private async Task<DeveloperFeeController> GetControllerForDeveloperFee()
        {
            var organizationInfoRet = await MainChainTester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub.GetDeveloperFeeController), new Empty());
            organizationInfoRet.Status.ShouldBe(TransactionResultStatus.Mined);
            var organizationInfo = new DeveloperFeeController();
            organizationInfo.MergeFrom(organizationInfoRet.ReturnValue);
            return organizationInfo;
        }
        
        private async Task<Address> GetDefaultParliamentAddress()
        {
            var parliamentOrgRet = await MainChainTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.GetDefaultOrganizationAddress), new Empty());
            parliamentOrgRet.Status.ShouldBe(TransactionResultStatus.Mined);
            var defaultParliamentAddress = new Address();
            defaultParliamentAddress.MergeFrom(parliamentOrgRet.ReturnValue);
            return defaultParliamentAddress;
        }

        private async Task IssuePrimaryTokenToMainChainTester()
        {
            var callOwner = Address.FromPublicKey(MainChainTester.KeyPair.PublicKey);
            var primaryTokenSymbol = await GetThePrimaryToken();

            var issueResult = await MainChainTester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub.Issue), new IssueInput
                {
                    Amount = 100000,
                    To = callOwner,
                    Symbol = primaryTokenSymbol
                });
            issueResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        private async Task<string> GetThePrimaryToken()
        {
            var primaryTokenRet = await MainChainTester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractImplContainer.TokenContractImplStub.GetPrimaryTokenSymbol), new Empty());
            var primaryTokenSymbol = new StringValue();
            primaryTokenSymbol.MergeFrom(primaryTokenRet.ReturnValue);
            return primaryTokenSymbol.Value;
        }
    }
}