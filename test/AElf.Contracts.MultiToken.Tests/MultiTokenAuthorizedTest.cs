using System.Linq;
using System.Threading.Tasks;
using Acs3;
using AElf.Contracts.Association;
using AElf.Contracts.Parliament;
using AElf.Contracts.Referendum;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.MultiToken
{
    public class MultiTokenAuthorizedTest : MultiTokenContractCrossChainTestBase
    {
        public MultiTokenAuthorizedTest()
        {
            AsyncHelper.RunSync(InitializeTokenContract);
        }

        private async Task InitializeTokenContract()
        {
            var initResult = await MainChainTester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.Initialize), new InitializeInput());
            initResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var initOrgResult = await MainChainTester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.InitializeAuthorizedOrganization), new Empty());
            initOrgResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task Update_Coefficient_For_Sender_Should_Success()
        {
            await CreateAndIssueVoteToken();
            const int pieceKey = 1000000;
            var updateInput = new CoefficientFromSender
            {
                LinerCoefficient = new LinerCoefficient
                {
                    ConstantValue = 1,
                    Denominator = 2,
                    Numerator = 3
                },
                PieceKey = pieceKey,
                IsLiner = true
            };
            var proposalId = await CreateToRootForUserFeeByTwoLayer(updateInput);
            await ApproveToRootForUserFeeByTwoLayer(proposalId);
            await VoteToReferendum(proposalId);
            await ReleaseToRootForUserFeeByTwoLayer(proposalId);
            
            var userCoefficientRet = await MainChainTester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.GetCalculateFeeCoefficientOfSender), new Empty());
            userCoefficientRet.Status.ShouldBe(TransactionResultStatus.Mined);
            var userCoefficient = new CalculateFeeCoefficientsOfType();
            userCoefficient.MergeFrom(userCoefficientRet.ReturnValue);
            var hasModified = userCoefficient.Coefficients.Single(x => x.PieceKey == pieceKey);
            hasModified.CoefficientDic["ConstantValue".ToLower()].ShouldBe(1);
            hasModified.CoefficientDic["Denominator".ToLower()].ShouldBe(2);
            hasModified.CoefficientDic["Numerator".ToLower()].ShouldBe(3);
        }

        [Fact]
        public async Task Update_Coefficient_For_Contract_Should_Success()
        {
            const int pieceKey = 1000000;
            const FeeTypeEnum feeType = FeeTypeEnum.Traffic;
            var updateInput = new CoefficientFromContract
            {
                FeeType = feeType,
                Coefficient = new CoefficientFromSender
                {
                    LinerCoefficient = new LinerCoefficient
                    {
                        ConstantValue = 1,
                        Denominator = 2,
                        Numerator = 3
                    },
                    PieceKey = pieceKey,
                    IsLiner = true
                }
            };

            var proposalId = await CreateToRootForDeveloperFeeByTwoLayer(updateInput);
            await ApproveToRootForDeveloperFeeByTwoLayer(proposalId);
            
            var middleApproveProposalId = await ApproveToRootForDeveloperFeeByMiddleLayer(proposalId);
            await ApproveThenReleaseMiddleProposalForDeveloper(middleApproveProposalId);
            
            await ReleaseToRootForDeveloperFeeByTwoLayer(proposalId);
            
            var developerCoefficientRet = await MainChainTester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.GetCalculateFeeCoefficientOfContract), new SInt32Value
                {
                    Value = (int) feeType
                });
            developerCoefficientRet.Status.ShouldBe(TransactionResultStatus.Mined);
            var userCoefficient = new CalculateFeeCoefficientsOfType();
            userCoefficient.MergeFrom(developerCoefficientRet.ReturnValue);
            var hasModified = userCoefficient.Coefficients.Single(x => x.PieceKey == pieceKey);
            hasModified.CoefficientDic["ConstantValue".ToLower()].ShouldBe(1);
            hasModified.CoefficientDic["Denominator".ToLower()].ShouldBe(2);
            hasModified.CoefficientDic["Numerator".ToLower()].ShouldBe(3);
        }
        
        private async Task<Hash> CreateToRootForDeveloperFeeByTwoLayer(CoefficientFromContract input)
        {
            var organizations = await GetControllerForDeveloperFee();
            var createNestProposalInput = new CreateProposalInput
            {
                ToAddress = TokenContractAddress,
                Params = input.ToByteString(),
                OrganizationAddress = organizations.RootController,
                ContractMethodName = nameof(TokenContractContainer.TokenContractStub.UpdateCoefficientFromContract),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            var createProposalInput = new CreateProposalInput
            {
                ToAddress = AssociationAddress,
                Params = createNestProposalInput.ToByteString(),
                OrganizationAddress = organizations.ParliamentController,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.CreateProposal),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            var parliamentCreateProposal = await MainChainTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.CreateProposal),
                createProposalInput);
            parliamentCreateProposal.Status.ShouldBe(TransactionResultStatus.Mined);
            var parliamentProposalId = new Hash();
            parliamentProposalId.MergeFrom(parliamentCreateProposal.ReturnValue);
            await ApproveWithMinersAsync(parliamentProposalId, ParliamentAddress, MainChainTester);
            var releaseRet = await ReleaseProposalAsync(parliamentProposalId, ParliamentAddress, MainChainTester);
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
                OrganizationAddress = organizations.ParliamentController,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.Approve),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            var parliamentCreateProposal = await MainChainTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.CreateProposal),
                approveProposalInput);
            parliamentCreateProposal.Status.ShouldBe(TransactionResultStatus.Mined);
            var parliamentProposalId = new Hash();
            parliamentProposalId.MergeFrom(parliamentCreateProposal.ReturnValue);
            await ApproveWithMinersAsync(parliamentProposalId, ParliamentAddress, MainChainTester);
            await ReleaseProposalAsync(parliamentProposalId, ParliamentAddress, MainChainTester);
        }
        
        private async Task ReleaseToRootForDeveloperFeeByTwoLayer(Hash input)
        {
            var organizations = await GetControllerForDeveloperFee();
            var releaseProposalInput = new CreateProposalInput
            {
                ToAddress = AssociationAddress,
                Params = input.ToByteString(),
                OrganizationAddress = organizations.ParliamentController,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.Release),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            var parliamentCreateProposal = await MainChainTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.CreateProposal),
                releaseProposalInput);
            parliamentCreateProposal.Status.ShouldBe(TransactionResultStatus.Mined);
            var parliamentProposalId = new Hash();
            parliamentProposalId.MergeFrom(parliamentCreateProposal.ReturnValue);
            await ApproveWithMinersAsync(parliamentProposalId, ParliamentAddress, MainChainTester);
            await ReleaseProposalAsync(parliamentProposalId, ParliamentAddress, MainChainTester);
        }
        
        private async Task<Hash> ApproveToRootForDeveloperFeeByMiddleLayer(Hash input)
        {
            var organizations = await GetControllerForDeveloperFee();
            var approveMidProposalInput = new CreateProposalInput
            {
                ToAddress = AssociationAddress,
                Params = input.ToByteString(),
                OrganizationAddress = organizations.DeveloperController,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.Approve),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            var approveLeafProposalInput = new CreateProposalInput
            {
                ToAddress = AssociationAddress,
                Params = approveMidProposalInput.ToByteString(),
                OrganizationAddress = organizations.ParliamentController,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.CreateProposal),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            var parliamentCreateProposal = await MainChainTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.CreateProposal),
                approveLeafProposalInput);
            parliamentCreateProposal.Status.ShouldBe(TransactionResultStatus.Mined);
            var parliamentProposalId = new Hash();
            parliamentProposalId.MergeFrom(parliamentCreateProposal.ReturnValue);
            await ApproveWithMinersAsync(parliamentProposalId, ParliamentAddress, MainChainTester);
            var newCreateProposalRet = await ReleaseProposalAsync(parliamentProposalId, ParliamentAddress, MainChainTester);

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
                OrganizationAddress = organizations.ParliamentController,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.Approve),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            var parliamentCreateProposal = await MainChainTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.CreateProposal),
                approveLeafProposalInput);
            parliamentCreateProposal.Status.ShouldBe(TransactionResultStatus.Mined);
            var parliamentProposalId = new Hash();
            parliamentProposalId.MergeFrom(parliamentCreateProposal.ReturnValue);
            await ApproveWithMinersAsync(parliamentProposalId, ParliamentAddress, MainChainTester);
            await ReleaseProposalAsync(parliamentProposalId, ParliamentAddress, MainChainTester);
            
            approveLeafProposalInput = new CreateProposalInput
            {
                ToAddress = AssociationAddress,
                Params = input.ToByteString(),
                OrganizationAddress = organizations.ParliamentController,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.Release),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            parliamentCreateProposal = await MainChainTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.CreateProposal),
                approveLeafProposalInput);
            parliamentCreateProposal.Status.ShouldBe(TransactionResultStatus.Mined);
            parliamentProposalId = new Hash();
            parliamentProposalId.MergeFrom(parliamentCreateProposal.ReturnValue);
            await ApproveWithMinersAsync(parliamentProposalId, ParliamentAddress, MainChainTester);
            await ReleaseProposalAsync(parliamentProposalId, ParliamentAddress, MainChainTester);
        }

        private async Task<Hash> CreateToRootForUserFeeByTwoLayer(CoefficientFromSender input)
        {
            var organizations = await GetControllerForUserFee();
            var createNestProposalInput = new CreateProposalInput
            {
                ToAddress = TokenContractAddress,
                Params = input.ToByteString(),
                OrganizationAddress = organizations.RootController,
                ContractMethodName = nameof(TokenContractContainer.TokenContractStub.UpdateCoefficientFromSender),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            
            var createProposalInput = new CreateProposalInput
            {
                ToAddress = AssociationAddress,
                Params = createNestProposalInput.ToByteString(),
                OrganizationAddress = organizations.ParliamentController,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.CreateProposal),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            var parliamentCreateProposal = await MainChainTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.CreateProposal),
                createProposalInput);
            parliamentCreateProposal.Status.ShouldBe(TransactionResultStatus.Mined);
            var parliamentProposalId = new Hash();
            parliamentProposalId.MergeFrom(parliamentCreateProposal.ReturnValue);
            await ApproveWithMinersAsync(parliamentProposalId, ParliamentAddress, MainChainTester);
            var releaseRet = await ReleaseProposalAsync(parliamentProposalId, ParliamentAddress, MainChainTester);
            
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
                OrganizationAddress = organizations.ParliamentController,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.Approve),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            var parliamentCreateProposal = await MainChainTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.CreateProposal),
                approveProposalInput);
            parliamentCreateProposal.Status.ShouldBe(TransactionResultStatus.Mined);
            var parliamentProposalId = new Hash();
            parliamentProposalId.MergeFrom(parliamentCreateProposal.ReturnValue);
            await ApproveWithMinersAsync(parliamentProposalId, ParliamentAddress, MainChainTester);
            await ReleaseProposalAsync(parliamentProposalId, ParliamentAddress, MainChainTester);
        }

        private async Task VoteToReferendum(Hash input)
        {
            var organizations = await GetControllerForUserFee();
            
            var referendumProposal = new CreateProposalInput
            {
                ToAddress = AssociationAddress,
                Params = input.ToByteString(),
                OrganizationAddress = organizations.ReferendumController,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.Approve),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            var parliamentProposal = new CreateProposalInput
            {
                ToAddress = ReferendumAddress,
                Params = referendumProposal.ToByteString(),
                OrganizationAddress = organizations.ParliamentController,
                ContractMethodName = nameof(ReferendumContractContainer.ReferendumContractStub.CreateProposal),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            var parliamentCreateProposal = await MainChainTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.CreateProposal),
                parliamentProposal);
            parliamentCreateProposal.Status.ShouldBe(TransactionResultStatus.Mined);
            var parliamentProposalId = new Hash();
            parliamentProposalId.MergeFrom(parliamentCreateProposal.ReturnValue);
            await ApproveWithMinersAsync(parliamentProposalId, ParliamentAddress, MainChainTester);
            var ret = await ReleaseProposalAsync(parliamentProposalId, ParliamentAddress, MainChainTester);
            var id = ProposalCreated.Parser
                .ParseFrom(ret.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                    .NonIndexed).ProposalId;
            await MainChainTester.ExecuteContractWithMiningAsync(ReferendumAddress,
                nameof(ReferendumContractContainer.ReferendumContractStub.Approve),
                id);
            
            parliamentProposal = new CreateProposalInput
            {
                ToAddress = ReferendumAddress,
                Params = id.ToByteString(),
                OrganizationAddress = organizations.ParliamentController,
                ContractMethodName = nameof(ReferendumContractContainer.ReferendumContractStub.Release),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            parliamentCreateProposal = await MainChainTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.CreateProposal),
                parliamentProposal);
            parliamentCreateProposal.Status.ShouldBe(TransactionResultStatus.Mined);
            parliamentProposalId = new Hash();
            parliamentProposalId.MergeFrom(parliamentCreateProposal.ReturnValue);
            await ApproveWithMinersAsync(parliamentProposalId, ParliamentAddress, MainChainTester);
            await ReleaseProposalAsync(parliamentProposalId, ParliamentAddress, MainChainTester);
        }

        private async Task ReleaseToRootForUserFeeByTwoLayer(Hash input)
        {
            var organizations = await GetControllerForUserFee();
            var parliamentProposal = new CreateProposalInput
            {
                ToAddress = AssociationAddress,
                Params = input.ToByteString(),
                OrganizationAddress = organizations.ParliamentController,
                ContractMethodName = nameof(AssociationContractContainer.AssociationContractStub.Release),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            };
            var parliamentCreateProposal = await MainChainTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.CreateProposal),
                parliamentProposal);
            parliamentCreateProposal.Status.ShouldBe(TransactionResultStatus.Mined);
            var parliamentProposalId = new Hash();
            parliamentProposalId.MergeFrom(parliamentCreateProposal.ReturnValue);
            await ApproveWithMinersAsync(parliamentProposalId, ParliamentAddress, MainChainTester);
            await ReleaseProposalAsync(parliamentProposalId, ParliamentAddress, MainChainTester);
        }
        private async Task<ControllerForUserFee> GetControllerForUserFee()
        {
            var organizationInfoRet = await MainChainTester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.GetUserFeeController), new Empty());
            organizationInfoRet.Status.ShouldBe(TransactionResultStatus.Mined);
            var organizationInfo = new ControllerForUserFee();
            organizationInfo.MergeFrom(organizationInfoRet.ReturnValue);
            return organizationInfo;
        }
        
        private async Task<ControllerForDeveloperFee> GetControllerForDeveloperFee()
        {
            var organizationInfoRet = await MainChainTester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.GetDeveloperFeeController), new Empty());
            organizationInfoRet.Status.ShouldBe(TransactionResultStatus.Mined);
            var organizationInfo = new ControllerForDeveloperFee();
            organizationInfo.MergeFrom(organizationInfoRet.ReturnValue);
            return organizationInfo;
        }

        private async Task CreateAndIssueVoteToken()
        {
            const string defaultSymbol = "EE";
            var callOwner = Address.FromPublicKey(MainChainTester.KeyPair.PublicKey);
            var createInput = new CreateInput
            {
                Symbol = defaultSymbol,
                TokenName = defaultSymbol,
                Decimals = 2,
                IsBurnable = true,
                TotalSupply = 10000_0000,
                Issuer = callOwner,
                LockWhiteList = {ReferendumAddress}
            };
            var createRet = await MainChainTester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.Create), createInput);
            createRet.Status.ShouldBe(TransactionResultStatus.Mined);
            var issueResult = await MainChainTester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.Issue), new IssueInput
                {
                    Amount = 50000000,
                    To = callOwner,
                    Symbol = defaultSymbol
                });
            issueResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var approveResult = await MainChainTester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.Approve), new ApproveInput
                {
                    Spender = ReferendumAddress,
                    Symbol = defaultSymbol,
                    Amount = 50000000
                });
            approveResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
    }
}