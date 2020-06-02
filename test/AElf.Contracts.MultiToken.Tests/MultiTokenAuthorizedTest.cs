using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs1;
using Acs3;
using AElf.Contracts.Association;
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
    public class MultiTokenAuthorizedTest : MultiTokenContractCrossChainTestBase
    {
        public MultiTokenAuthorizedTest()
        {
            AsyncHelper.RunSync(() => TokenContractStub.InitializeAuthorizedController.SendAsync(new Empty()));
        }

        [Fact]
        public async Task Controller_Transfer_For_Symbol_To_Pay_Tx_Fee()
        {
            var primarySymbol = await TokenContractStub.GetPrimaryTokenSymbol.CallAsync(new Empty());
            var newSymbolList = new SymbolListToPayTxSizeFee();
            newSymbolList.SymbolsToPayTxSizeFee.Add(new SymbolToPayTxSizeFee
            {
                TokenSymbol = primarySymbol.Value,
                AddedTokenWeight = 1,
                BaseTokenWeight = 1
            });
            ;
            var symbolSetResult = await TokenContractStub.SetSymbolsToPayTxSizeFee.SendWithExceptionAsync(newSymbolList);
            symbolSetResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            
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

            var parliamentCreateResult = await ParliamentContractStub.CreateOrganization.SendAsync(newParliament);
            var newParliamentAddress = parliamentCreateResult.Output;
            var newAuthority = new AuthorityInfo
            {
                ContractAddress = ParliamentAddress,
                OwnerAddress = newParliamentAddress
            };
            var defaultParliamentAddress =
                await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        
            var createProposalInput = new CreateProposalInput
            {
                ToAddress = TokenContractAddress,
                Params = newAuthority.ToByteString(),
                OrganizationAddress = defaultParliamentAddress,
                ContractMethodName = nameof(TokenContractImplContainer.TokenContractImplStub
                    .ChangeSymbolsToPayTXSizeFeeController),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            
            var parliamentCreateProposalResult = await ParliamentContractStub.CreateProposal.SendAsync(createProposalInput);
            var parliamentProposalId = parliamentCreateProposalResult.Output;

            await ApproveWithMinersAsync(parliamentProposalId);
            await ReleaseProposalAsync(parliamentProposalId);
        
            var updateInput = new CreateProposalInput
            {
                ToAddress = TokenContractAddress,
                Params = newSymbolList.ToByteString(),
                OrganizationAddress = newParliamentAddress,
                ContractMethodName = nameof(TokenContractImplContainer.TokenContractImplStub.SetSymbolsToPayTxSizeFee),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            var updateProposalResult = await ParliamentContractStub.CreateProposal.SendAsync(updateInput);
            var updateProposalId = updateProposalResult.Output;
        
            await ParliamentContractStub.Approve.SendAsync(updateProposalId);
            await ParliamentContractStub.Release.SendAsync(updateProposalId);
            
            var updatedSymbolList = await TokenContractStub.GetSymbolsToPayTxSizeFee.CallAsync(new Empty());
            updatedSymbolList.SymbolsToPayTxSizeFee.Count.ShouldBe(1);
        }
        
        [Fact]
        public async Task Update_Coefficient_For_Sender()
        {
            await CreateAndIssueVoteToken();
            const int pieceUpperBound = 1000000;
            var updateInput = new UpdateCoefficientsInput
            {
                PieceNumbers = {1},
                Coefficients = new CalculateFeeCoefficients
                {
                    PieceCoefficientsList =
                    {
                        new CalculateFeePieceCoefficients
                        {
                            Value = {pieceUpperBound, 4, 3, 2}
                        }
                    }
                }
            };
            var proposalId = await CreateToRootForUserFeeByTwoLayer(updateInput);
            await ApproveToRootForUserFeeByTwoLayer(proposalId);
            await VoteToReferendum(proposalId);
            await ReleaseToRootForUserFeeByTwoLayer(proposalId);

            var userCoefficient = await TokenContractStub.GetCalculateFeeCoefficientsForSender.CallAsync(new Empty());
            var hasModified = GetCalculateFeePieceCoefficients(userCoefficient.PieceCoefficientsList, pieceUpperBound);
            hasModified.Value.Skip(1).ShouldBe(new[] {4, 3, 2});
        }
        
        [Fact]
        public async Task Update_Coefficient_For_Contract_Should_Success()
        {
            const int pieceUpperBound = 1000000;
            const FeeTypeEnum feeType = FeeTypeEnum.Traffic;
            var updateInput = new UpdateCoefficientsInput
            {
                PieceNumbers = {1},
                Coefficients = new CalculateFeeCoefficients
                {
                    FeeTokenType = (int) feeType,
                    PieceCoefficientsList =
                    {
                        new CalculateFeePieceCoefficients
                        {
                            Value = {pieceUpperBound, 4, 3, 2}
                        }
                    }
                }
            };
            var proposalId = await CreateToRootForDeveloperFeeByTwoLayer(updateInput);
            await ApproveToRootForDeveloperFeeByTwoLayer(proposalId);
        
            var middleApproveProposalId = await ApproveToRootForDeveloperFeeByMiddleLayer(proposalId);
            await ApproveThenReleaseMiddleProposalForDeveloper(middleApproveProposalId);
        
            await ReleaseToRootForDeveloperFeeByTwoLayer(proposalId);

            var userCoefficient = await TokenContractStub.GetCalculateFeeCoefficientsForContract.CallAsync(new Int32Value
            {
                Value = (int) feeType
            });;
            var hasModified = GetCalculateFeePieceCoefficients(userCoefficient.PieceCoefficientsList, pieceUpperBound);
            hasModified.Value.Skip(1).ShouldBe(new[] {4, 3, 2});
        }
        
        [Fact]
        public async Task Update_Coefficient_PieceUpperBound_Test()
        {
            const int newPieceUpperBound = 999999;
            const FeeTypeEnum feeType = FeeTypeEnum.Read;
            var updateInput = new UpdateCoefficientsInput
            {
                PieceNumbers = {2},
                Coefficients = new CalculateFeeCoefficients
                {
                    FeeTokenType = (int) feeType,
                    PieceCoefficientsList =
                    {
                        new CalculateFeePieceCoefficients
                        {
                            Value = {newPieceUpperBound, 1, 4, 2, 5, 250, 40}
                        }
                    }
                }
            };
            var proposalId = await CreateToRootForDeveloperFeeByTwoLayer(updateInput);
            await ApproveToRootForDeveloperFeeByTwoLayer(proposalId);
        
            var middleApproveProposalId = await ApproveToRootForDeveloperFeeByMiddleLayer(proposalId);
            await ApproveThenReleaseMiddleProposalForDeveloper(middleApproveProposalId);
        
            await ReleaseToRootForDeveloperFeeByTwoLayer(proposalId);

            var userCoefficient = await TokenContractStub.GetCalculateFeeCoefficientsForContract.CallAsync(new Int32Value
            {
                Value = (int) feeType
            });
            var hasModified =
                GetCalculateFeePieceCoefficients(userCoefficient.PieceCoefficientsList, newPieceUpperBound);
            hasModified.ShouldNotBeNull();
        }
        
        [Fact]
        public async Task Update_Coefficient_PowerAlgorithm_Test()
        {
            const int pieceUpperBound = int.MaxValue;
            const FeeTypeEnum feeType = FeeTypeEnum.Read;
            var updateInput = new UpdateCoefficientsInput
            {
                PieceNumbers = {3},
                Coefficients = new CalculateFeeCoefficients
                {
                    FeeTokenType = (int) feeType,
                    PieceCoefficientsList =
                    {
                        new CalculateFeePieceCoefficients
                        {
                            Value = {pieceUpperBound, 2, 8, 2, 6, 300, 50}
                        }
                    }
                }
            };
            var proposalId = await CreateToRootForDeveloperFeeByTwoLayer(updateInput);
            await ApproveToRootForDeveloperFeeByTwoLayer(proposalId);
        
            var middleApproveProposalId = await ApproveToRootForDeveloperFeeByMiddleLayer(proposalId);
            await ApproveThenReleaseMiddleProposalForDeveloper(middleApproveProposalId);
        
            await ReleaseToRootForDeveloperFeeByTwoLayer(proposalId);

            var userCoefficient = await TokenContractStub.GetCalculateFeeCoefficientsForContract.CallAsync(new Int32Value
            {
                Value = (int) feeType
            });
            var hasModified = GetCalculateFeePieceCoefficients(userCoefficient.PieceCoefficientsList, pieceUpperBound);
            hasModified.Value.Skip(1).ShouldBe(new[] {2, 8, 2, 6, 300, 50});
        }
        
        [Fact]
        public async Task Update_Coefficient_Multiple_Algorithm_Test()
        {
            const int pieceUpperBound = int.MaxValue;
            const FeeTypeEnum feeType = FeeTypeEnum.Write;
            var updateInput = new UpdateCoefficientsInput
            {
                PieceNumbers = {2, 3},
                Coefficients = new CalculateFeeCoefficients
                {
                    FeeTokenType = (int) feeType,
                    PieceCoefficientsList =
                    {
                        new CalculateFeePieceCoefficients
                        {
                            Value = {100, 1, 4, 10000}
                        },
                        new CalculateFeePieceCoefficients
                        {
                            Value = {1000000, 1, 4, 2, 2, 250, 50}
                        }
                    }
                }
            };
            var proposalId = await CreateToRootForDeveloperFeeByTwoLayer(updateInput);
            await ApproveToRootForDeveloperFeeByTwoLayer(proposalId);
            var middleApproveProposalId = await ApproveToRootForDeveloperFeeByMiddleLayer(proposalId);
            await ApproveThenReleaseMiddleProposalForDeveloper(middleApproveProposalId);
            await ReleaseToRootForDeveloperFeeByTwoLayer(proposalId);
        
            var userCoefficient = await TokenContractStub.GetCalculateFeeCoefficientsForContract.CallAsync(
                new Int32Value
                {
                    Value = (int) feeType
                });
            var hasModified = GetCalculateFeePieceCoefficients(userCoefficient.PieceCoefficientsList, 100);
            hasModified.Value.Skip(1).ShouldBe(new[] {1, 4, 10000});
        }
        
        [Fact]
        public async Task Update_Coefficient_PowerAlgorithm_Should_Fail_Test()
        {
            const int pieceUpperBound = int.MaxValue;
            const int feeType = (int) FeeTypeEnum.Read;
            const int checkValue = 1000;
            var updateInvalidPieceKeyInput = new UpdateCoefficientsInput
            {
                PieceNumbers = {2},
                Coefficients = new CalculateFeeCoefficients
                {
                    FeeTokenType = feeType,
                    PieceCoefficientsList =
                    {
                        new CalculateFeePieceCoefficients
                        {
                            Value = {pieceUpperBound, checkValue, checkValue, checkValue}
                        }
                    }
                }
            };
            await InvalidUpdateCoefficient(updateInvalidPieceKeyInput, feeType, pieceUpperBound, checkValue);
            updateInvalidPieceKeyInput = new UpdateCoefficientsInput
            {
                PieceNumbers = {3, 2},
                Coefficients = new CalculateFeeCoefficients
                {
                    FeeTokenType = feeType,
                    PieceCoefficientsList =
                    {
                        new CalculateFeePieceCoefficients
                        {
                            Value = {pieceUpperBound, checkValue, checkValue, checkValue}
                        },
                        new CalculateFeePieceCoefficients
                        {
                            Value = {pieceUpperBound, checkValue, checkValue, checkValue}
                        }
                    }
                }
            };
            await InvalidUpdateCoefficient(updateInvalidPieceKeyInput, feeType, pieceUpperBound, checkValue);
        }
        
        private CalculateFeePieceCoefficients GetCalculateFeePieceCoefficients(
            IEnumerable<CalculateFeePieceCoefficients> calculateFeePieceCoefficients, int pieceUpperBound)
        {
            return calculateFeePieceCoefficients.SingleOrDefault(c => c.Value[0] == pieceUpperBound);
        }
        
        private async Task InvalidUpdateCoefficient(UpdateCoefficientsInput input, int feeType, int pieceUpperBound,
            int checkValue)
        {
            var proposalId = await CreateToRootForDeveloperFeeByTwoLayer(input);
            await ApproveToRootForDeveloperFeeByTwoLayer(proposalId);
        
            var middleApproveProposalId = await ApproveToRootForDeveloperFeeByMiddleLayer(proposalId);
            await ApproveThenReleaseMiddleProposalForDeveloper(middleApproveProposalId);
        
            await ReleaseToRootForDeveloperFeeByTwoLayer(proposalId);
        
            var userCoefficient = await TokenContractStub.GetCalculateFeeCoefficientsForContract.CallAsync(
                new Int32Value
                {
                    Value = feeType
                });
            var hasModified = GetCalculateFeePieceCoefficients(userCoefficient.PieceCoefficientsList, pieceUpperBound);
            hasModified.Value[2].ShouldNotBe(checkValue);
            hasModified.Value[3].ShouldNotBe(checkValue);
            hasModified.Value[4].ShouldNotBe(checkValue);
        }
        
        [Fact]
        public async Task MethodFeeController_Test()
        {
            var defaultController = await TokenContractStub.GetMethodFeeController.CallAsync(new Empty());

            var createOrganizationResult = await ParliamentContractStub.CreateOrganization.SendAsync(
                new CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MinimalApprovalThreshold = 1000,
                        MinimalVoteThreshold = 1000
                    }
                });
            var organizationAddress = createOrganizationResult.Output;
        
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
                ContractMethodName = nameof(TokenContractStub.ChangeMethodFeeController),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            var parliamentCreateProposalResult = await ParliamentContractStub.CreateProposal.SendAsync(createProposalInput);
            var parliamentProposalId = parliamentCreateProposalResult.Output;
            await ApproveWithMinersAsync(parliamentProposalId);
            await ReleaseProposalAsync(parliamentProposalId);

            var queryController = await TokenContractStub.GetMethodFeeController.CallAsync(new Empty());
            queryController.ShouldBe(newController);
        }
        
        [Fact]
        public async Task Change_CrossChainTokenContract_RegistrationController_Test()
        {
            var createOrganizationResult = await ParliamentContractStub.CreateOrganization.SendAsync(
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
        
            var newOrganization = createOrganizationResult.Output;
        
            var defaultController =
                await TokenContractStub.GetCrossChainTokenContractRegistrationController.CallAsync(new Empty());
            var defaultOrganization =  await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
            defaultController.OwnerAddress.ShouldBe(defaultOrganization);
        
            const string proposalCreationMethodName = nameof(TokenContractImplContainer.TokenContractImplStub
                .ChangeCrossChainTokenContractRegistrationController);
            var newAuthority = new AuthorityInfo
            {
                ContractAddress = ParliamentAddress,
                OwnerAddress = newOrganization
            };
            var proposalId = await CreateProposalAsync(ParliamentContractStub,proposalCreationMethodName, newAuthority.ToByteString(),
                TokenContractAddress);
        
            await ApproveWithMinersAsync(proposalId);
            await ReleaseProposalAsync(proposalId);
        
            var newController =
                await TokenContractStub.GetCrossChainTokenContractRegistrationController.CallAsync(new Empty());
            Assert.True(newController.OwnerAddress == newOrganization);
        }
        
        [Fact]
        public async Task Change_CrossChainTokenContract_RegistrationController_WithoutAuth_Test()
        {
            var createOrganizationResult = await ParliamentContractStub.CreateOrganization.SendAsync(
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
        
            var newOrganization = createOrganizationResult.Output;
            var newAuthority = new AuthorityInfo
            {
                ContractAddress = ParliamentAddress,
                OwnerAddress = newOrganization
            };
            var executionResult = await TokenContractStub.ChangeCrossChainTokenContractRegistrationController.SendWithExceptionAsync(newAuthority);
            executionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            executionResult.TransactionResult.Error.Contains("No permission.").ShouldBeTrue();
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
            var parliamentCreateProposalResult = await ParliamentContractStub.CreateProposal.SendAsync(createProposalInput);
            var parliamentProposalId = parliamentCreateProposalResult.Output;
            await ApproveWithMinersAsync(parliamentProposalId);
            var releaseRet = await ReleaseProposalAsync(parliamentProposalId);
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
            var parliamentCreateProposalResult = await ParliamentContractStub.CreateProposal.SendAsync(approveProposalInput);
            var parliamentProposalId = parliamentCreateProposalResult.Output;
            await ApproveWithMinersAsync(parliamentProposalId);
            await ReleaseProposalAsync(parliamentProposalId);
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
            var parliamentCreateProposalResult = await ParliamentContractStub.CreateProposal.SendAsync(releaseProposalInput);
            var parliamentProposalId = parliamentCreateProposalResult.Output;
            await ApproveWithMinersAsync(parliamentProposalId);
            await ReleaseProposalAsync(parliamentProposalId);
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
            var parliamentCreateProposalResult =
                await ParliamentContractStub.CreateProposal.SendAsync(approveLeafProposalInput);
            var parliamentProposalId = parliamentCreateProposalResult.Output;
            await ApproveWithMinersAsync(parliamentProposalId);
            var newCreateProposalRet =
                await ReleaseProposalAsync(parliamentProposalId);
        
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
            var parliamentCreateProposalResult = await ParliamentContractStub.CreateProposal.SendAsync(approveLeafProposalInput);
            var parliamentProposalId = parliamentCreateProposalResult.Output;
            await ApproveWithMinersAsync(parliamentProposalId);
            await ReleaseProposalAsync(parliamentProposalId);
        
            approveLeafProposalInput = new CreateProposalInput
            {
                ToAddress = AssociationAddress,
                Params = input.ToByteString(),
                OrganizationAddress = organizations.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.Release),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            parliamentCreateProposalResult = await ParliamentContractStub.CreateProposal.SendAsync(approveLeafProposalInput);
            parliamentProposalId = parliamentCreateProposalResult.Output;
            await ApproveWithMinersAsync(parliamentProposalId);
            await ReleaseProposalAsync(parliamentProposalId);
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
            var parliamentCreateProposalResult = await ParliamentContractStub.CreateProposal.SendAsync(createProposalInput);
            var parliamentProposalId = parliamentCreateProposalResult.Output;
            await ApproveWithMinersAsync(parliamentProposalId);
            var releaseRet = await ReleaseProposalAsync(parliamentProposalId);
        
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
            var parliamentCreateProposalResult = await ParliamentContractStub.CreateProposal.SendAsync(approveProposalInput);
            var parliamentProposalId = parliamentCreateProposalResult.Output;
            await ApproveWithMinersAsync(parliamentProposalId);
            await ReleaseProposalAsync(parliamentProposalId);
        }
        
        private async Task VoteToReferendum(Hash input)
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
            var parliamentCreateProposalResult = await ParliamentContractStub.CreateProposal.SendAsync(parliamentProposal);
            var parliamentProposalId = parliamentCreateProposalResult.Output;
            await ApproveWithMinersAsync(parliamentProposalId);
            var ret = await ReleaseProposalAsync(parliamentProposalId);
            var id = ProposalCreated.Parser
                .ParseFrom(ret.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                    .NonIndexed).ProposalId;
            await ReferendumContractStub.Approve.SendAsync(id);
        
            parliamentProposal = new CreateProposalInput
            {
                ToAddress = ReferendumAddress,
                Params = id.ToByteString(),
                OrganizationAddress = organizations.ParliamentController.OwnerAddress,
                ContractMethodName = nameof(ReferendumContractContainer.ReferendumContractStub.Release),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            parliamentCreateProposalResult = await ParliamentContractStub.CreateProposal.SendAsync(parliamentProposal);
            parliamentProposalId = parliamentCreateProposalResult.Output;
            await ApproveWithMinersAsync(parliamentProposalId);
            await ReleaseProposalAsync(parliamentProposalId);
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
            var parliamentCreateProposalResult = await ParliamentContractStub.CreateProposal.SendAsync(parliamentProposal);
            var parliamentProposalId = parliamentCreateProposalResult.Output;
            await ApproveWithMinersAsync(parliamentProposalId);
            await ReleaseProposalAsync(parliamentProposalId);
        }
        
        private async Task<UserFeeController> GetControllerForUserFee()
        {
            return await TokenContractStub.GetUserFeeController.CallAsync(new Empty());
        }
        
        private async Task<DeveloperFeeController> GetControllerForDeveloperFee()
        {
            return await TokenContractStub.GetDeveloperFeeController.CallAsync(new Empty());
        }
        
        private async Task CreateAndIssueVoteToken()
        {
            var callOwner = Address.FromPublicKey(DefaultAccount.KeyPair.PublicKey);
            var symbol = await TokenContractStub.GetPrimaryTokenSymbol.CallAsync(new Empty());

            await TokenContractStub.Issue.SendAsync(new IssueInput
            {
                Amount = 100000,
                To = callOwner,
                Symbol = symbol.Value
            });

            await TokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Spender = ReferendumAddress,
                Symbol = symbol.Value,
                Amount = 100000
            });
        }
    }
}