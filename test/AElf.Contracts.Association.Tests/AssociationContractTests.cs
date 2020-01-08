using System.Linq;
using System.Threading.Tasks;
using Acs3;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Association
{
    public class AssociationContractTests : AssociationContractTestBase<AssociationContractTestAElfModule>
    {
        public AssociationContractTests()
        {
            DeployContracts();
        }

        [Fact]
        public async Task Get_Organization_Test()
        {
            //failed case
            {
                var organization =
                    await AssociationContractStub.GetOrganization.CallAsync(SampleAddress.AddressList[0]);
                organization.ShouldBe(new Organization());
            }

            //normal case
            {
                var minimalApproveThreshold = 2;
                var minimalVoteThreshold = 3;
                var maximalAbstentionThreshold = 1;
                var maximalRejectionThreshold = 1;
                var createOrganizationInput = new CreateOrganizationInput
                {
                    OrganizationMemberList = new OrganizationMemberList
                    {
                        OrganizationMembers = {Reviewer1, Reviewer2, Reviewer3}
                    },
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MinimalApprovalThreshold = minimalApproveThreshold,
                        MinimalVoteThreshold = minimalVoteThreshold,
                        MaximalAbstentionThreshold = maximalAbstentionThreshold,
                        MaximalRejectionThreshold = maximalRejectionThreshold
                    },
                    ProposerWhiteList = new ProposerWhiteList
                    {
                        Proposers = {Reviewer1}
                    }
                };
                var transactionResult =
                    await AssociationContractStub.CreateOrganization.SendAsync(createOrganizationInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                var organizationAddress = transactionResult.Output;
                var getOrganization = await AssociationContractStub.GetOrganization.CallAsync(organizationAddress);

                getOrganization.OrganizationAddress.ShouldBe(organizationAddress);
                getOrganization.ProposerWhiteList.ShouldBe(createOrganizationInput.ProposerWhiteList);
                getOrganization.ProposalReleaseThreshold.ShouldBe(createOrganizationInput.ProposalReleaseThreshold);
                getOrganization.OrganizationMemberList.ShouldBe(createOrganizationInput.OrganizationMemberList);
                getOrganization.OrganizationHash.ShouldBe(Hash.FromMessage(createOrganizationInput));
            }
        }
        
        [Fact]
        public async Task Create_OrganizationOnMultiChains_Test()
        {
            //failed case
            {
                var organization =
                    await AssociationContractStub.GetOrganization.CallAsync(SampleAddress.AddressList[0]);
                organization.ShouldBe(new Organization());
            }

            //normal case
            {
                var minimalApproveThreshold = 2;
                var minimalVoteThreshold = 3;
                var maximalAbstentionThreshold = 1;
                var maximalRejectionThreshold = 1;
                var createOrganizationInput = new CreateOrganizationInput
                {
                    OrganizationMemberList = new OrganizationMemberList
                    {
                        OrganizationMembers = {Reviewer1, Reviewer2, Reviewer3}
                    },
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MinimalApprovalThreshold = minimalApproveThreshold,
                        MinimalVoteThreshold = minimalVoteThreshold,
                        MaximalAbstentionThreshold = maximalAbstentionThreshold,
                        MaximalRejectionThreshold = maximalRejectionThreshold
                    },
                    ProposerWhiteList = new ProposerWhiteList
                    {
                        Proposers = {Reviewer1}
                    }
                };
                var transactionResult =
                    await AssociationContractStub.CreateOrganization.SendAsync(createOrganizationInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                var organizationAddress = transactionResult.Output;

                var organizationAddressOnAnotherChain =
                    await new SideChainAssociationContractTestBase().CreateOrganization(createOrganizationInput);
                organizationAddressOnAnotherChain.ShouldBe(organizationAddress);
            }
        }

        [Fact]
        public async Task Get_Proposal_Test()
        {
            //failed case
            {
                var proposal = await AssociationContractStub.GetProposal.CallAsync(Hash.FromString("Test"));
                proposal.ShouldBe(new ProposalOutput());
            }

            //normal case
            {
                var minimalApproveThreshold = 2;
                var minimalVoteThreshold = 3;
                var maximalAbstentionThreshold = 1;
                var maximalRejectionThreshold = 1;
                var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
                    maximalAbstentionThreshold, maximalRejectionThreshold, Reviewer1);
                var transferInput = new TransferInput()
                {
                    Symbol = "ELF",
                    Amount = 100,
                    To = Reviewer1,
                    Memo = "Transfer"
                };
                var proposalId = await CreateProposalAsync(Reviewer1KeyPair, organizationAddress);
                var getProposal = await AssociationContractStub.GetProposal.SendAsync(proposalId);

                getProposal.Output.Proposer.ShouldBe(Reviewer1);
                getProposal.Output.ContractMethodName.ShouldBe(nameof(TokenContractStub.Transfer));
                getProposal.Output.ProposalId.ShouldBe(proposalId);
                getProposal.Output.OrganizationAddress.ShouldBe(organizationAddress);
                getProposal.Output.ToAddress.ShouldBe(TokenContractAddress);
                getProposal.Output.Params.ShouldBe(transferInput.ToByteString());
            }
        }

        [Fact]
        public async Task Create_Organization_Failed_Test()
        {
            //invalid MinimalApprovalThreshold
            {
                var minimalApproveThreshold = 0;
                var minimalVoteThreshold = 3;
                var maximalAbstentionThreshold = 0;
                var maximalRejectionThreshold = 0;

                var createOrganizationInput = GenerateCreateOrganizationInput(minimalApproveThreshold,
                    minimalVoteThreshold,
                    maximalAbstentionThreshold, maximalRejectionThreshold, Reviewer1);

                var transactionResult =
                    await AssociationContractStub.CreateOrganization.SendWithExceptionAsync(createOrganizationInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
            }

            //invalid MinimalApprovalThreshold
            {
                var minimalApproveThreshold = 4;
                var minimalVoteThreshold = 3;
                var maximalAbstentionThreshold = 0;
                var maximalRejectionThreshold = 0;


                var createOrganizationInput = GenerateCreateOrganizationInput(minimalApproveThreshold,
                    minimalVoteThreshold,
                    maximalAbstentionThreshold, maximalRejectionThreshold, Reviewer1);

                var transactionResult =
                    await AssociationContractStub.CreateOrganization.SendWithExceptionAsync(createOrganizationInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
            }

            //invalid MinimalApprovalThreshold
            {
                var minimalApproveThreshold = 2;
                var minimalVoteThreshold = 1;
                var maximalAbstentionThreshold = 0;
                var maximalRejectionThreshold = 0;

                var createOrganizationInput = GenerateCreateOrganizationInput(minimalApproveThreshold,
                    minimalVoteThreshold,
                    maximalAbstentionThreshold, maximalRejectionThreshold, Reviewer1);

                var transactionResult =
                    await AssociationContractStub.CreateOrganization.SendWithExceptionAsync(createOrganizationInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
            }

            //invalid minimalVoteThreshold
            {
                var minimalApproveThreshold = 1;
                var minimalVoteThreshold = 0;
                var maximalAbstentionThreshold = 0;
                var maximalRejectionThreshold = 0;

                var createOrganizationInput = GenerateCreateOrganizationInput(minimalApproveThreshold,
                    minimalVoteThreshold,
                    maximalAbstentionThreshold, maximalRejectionThreshold, Reviewer1);
                var transactionResult =
                    await AssociationContractStub.CreateOrganization.SendWithExceptionAsync(createOrganizationInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
            }

            //invalid maximalAbstentionThreshold
            {
                var minimalApproveThreshold = 1;
                var minimalVoteThreshold = 3;
                var maximalAbstentionThreshold = 4;
                var maximalRejectionThreshold = 0;

                var createOrganizationInput = GenerateCreateOrganizationInput(minimalApproveThreshold,
                    minimalVoteThreshold,
                    maximalAbstentionThreshold, maximalRejectionThreshold, Reviewer1);
                var transactionResult =
                    await AssociationContractStub.CreateOrganization.SendWithExceptionAsync(createOrganizationInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
            }

            //invalid minimalVoteThreshold + maximalAbstentionThreshold
            {
                var minimalApproveThreshold = 3;
                var minimalVoteThreshold = 3;
                var maximalAbstentionThreshold = 1;
                var maximalRejectionThreshold = 0;

                var createOrganizationInput = GenerateCreateOrganizationInput(minimalApproveThreshold,
                    minimalVoteThreshold,
                    maximalAbstentionThreshold, maximalRejectionThreshold, Reviewer1);
                var transactionResult =
                    await AssociationContractStub.CreateOrganization.SendWithExceptionAsync(createOrganizationInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
            }

            //invalid minimalVoteThreshold + maximalRejectionThreshold
            {
                var minimalApproveThreshold = 3;
                var minimalVoteThreshold = 3;
                var maximalAbstentionThreshold = 0;
                var maximalRejectionThreshold = 1;

                var createOrganizationInput = GenerateCreateOrganizationInput(minimalApproveThreshold,
                    minimalVoteThreshold,
                    maximalAbstentionThreshold, maximalRejectionThreshold, Reviewer1);
                var transactionResult =
                    await AssociationContractStub.CreateOrganization.SendWithExceptionAsync(createOrganizationInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
            }

            // empty proposer white list
            {
                var minimalApproveThreshold = 1;
                var minimalVoteThreshold = 2;
                var maximalAbstentionThreshold = 0;
                var maximalRejectionThreshold = 0;

                var createOrganizationInput = GenerateCreateOrganizationInput(minimalApproveThreshold,
                    minimalVoteThreshold,
                    maximalAbstentionThreshold, maximalRejectionThreshold);
                var transactionResult =
                    await AssociationContractStub.CreateOrganization.SendWithExceptionAsync(createOrganizationInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
            }

            // empty organization members
            {
                var minimalApproveThreshold = 1;
                var minimalVoteThreshold = 2;
                var maximalAbstentionThreshold = 0;
                var maximalRejectionThreshold = 0;

                var createOrganizationInput = GenerateCreateOrganizationInput(minimalApproveThreshold,
                    minimalVoteThreshold,
                    maximalAbstentionThreshold, maximalRejectionThreshold, Reviewer1);
                createOrganizationInput.OrganizationMemberList = new OrganizationMemberList();
                var transactionResult =
                    await AssociationContractStub.CreateOrganization.SendWithExceptionAsync(createOrganizationInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
            }
        }

        [Fact]
        public async Task Create_Proposal_Failed_Test()
        {
            var minimalApproveThreshold = 2;
            var minimalVoteThreshold = 3;
            var maximalAbstentionThreshold = 1;
            var maximalRejectionThreshold = 1;
            var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
                maximalAbstentionThreshold, maximalRejectionThreshold, Reviewer1);
            var associationContractStub = GetAssociationContractTester(Reviewer1KeyPair);
            var blockTime = BlockTimeProvider.GetBlockTime();
            var createProposalInput = new CreateProposalInput
            {
                ToAddress = SampleAddress.AddressList[0],
                Params = ByteString.CopyFromUtf8("Test"),
                ExpiredTime = blockTime.AddDays(1),
                OrganizationAddress = organizationAddress
            };
            //"Invalid proposal."
            //ContractMethodName is null or white space
            {
                var transactionResult =
                    await associationContractStub.CreateProposal.SendWithExceptionAsync(createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
            }
            //ToAddress is null
            {
                createProposalInput.ContractMethodName = "Test";
                createProposalInput.ToAddress = null;

                var transactionResult =
                    await associationContractStub.CreateProposal.SendWithExceptionAsync(createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
            }
            //ExpiredTime is null
            {
                createProposalInput.ExpiredTime = null;
                createProposalInput.ToAddress = SampleAddress.AddressList[0];

                var transactionResult =
                    await associationContractStub.CreateProposal.SendWithExceptionAsync(createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
            }
            //"Expired proposal."
            {
                createProposalInput.ExpiredTime = blockTime.AddMilliseconds(5);
                BlockTimeProvider.SetBlockTime(TimestampHelper.GetUtcNow().AddSeconds(10));

                var transactionResult =
                    await associationContractStub.CreateProposal.SendWithExceptionAsync(createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            }
            //"No registered organization."
            {
                createProposalInput.ExpiredTime = BlockTimeProvider.GetBlockTime().AddDays(1);
                createProposalInput.OrganizationAddress = SampleAddress.AddressList[1];

                var transactionResult =
                    await associationContractStub.CreateProposal.SendWithExceptionAsync(createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("No registered organization.").ShouldBeTrue();
            }
            //"Unable to propose."
            //Reviewer is not permission to propose
            {
                createProposalInput.OrganizationAddress = organizationAddress;
                var anotherAssociationContractStub = GetAssociationContractTester(DefaultSenderKeyPair);

                var transactionResult =
                    await anotherAssociationContractStub.CreateProposal.SendWithExceptionAsync(createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            }

            //"Proposal with same input."
            {
                var transactionResult1 =
                    await associationContractStub.CreateProposal.SendAsync(createProposalInput);
                transactionResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var transactionResult2 =
                    await associationContractStub.CreateProposal.SendAsync(createProposalInput);
                transactionResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
        }

        [Fact]
        public async Task Approve_Proposal_NotFoundProposal_Test()
        {
            var transactionResult =
                await AssociationContractStub.Approve.SendWithExceptionAsync(Hash.FromString("Test"));
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        }

        [Fact]
        public async Task Approve_Proposal_NotAuthorizedApproval_Test()
        {
            var minimalApproveThreshold = 2;
            var minimalVoteThreshold = 3;
            var maximalAbstentionThreshold = 1;
            var maximalRejectionThreshold = 1;
            var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
                maximalAbstentionThreshold, maximalRejectionThreshold, Reviewer1);

            var proposalId = await CreateProposalAsync(Reviewer1KeyPair, organizationAddress);
            var associationContractStub = GetAssociationContractTester(DefaultSenderKeyPair);
            var transactionResult = await associationContractStub.Approve.SendWithExceptionAsync(proposalId);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        }

        [Fact]
        public async Task Approve_Proposal_ExpiredTime_Test()
        {
            var minimalApproveThreshold = 2;
            var minimalVoteThreshold = 3;
            var maximalAbstentionThreshold = 1;
            var maximalRejectionThreshold = 1;
            var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
                maximalAbstentionThreshold, maximalRejectionThreshold, Reviewer1);
            var proposalId = await CreateProposalAsync(Reviewer1KeyPair, organizationAddress);
            AssociationContractStub = GetAssociationContractTester(Reviewer1KeyPair);
            BlockTimeProvider.SetBlockTime(BlockTimeProvider.GetBlockTime().AddDays(5));
            var error = await AssociationContractStub.Approve.CallWithExceptionAsync(proposalId);
            error.Value.ShouldContain("Invalid proposal.");
        }

        [Fact]
        public async Task Approve_Proposal_ApprovalAlreadyExists_Test()
        {
            var minimalApproveThreshold = 2;
            var minimalVoteThreshold = 3;
            var maximalAbstentionThreshold = 1;
            var maximalRejectionThreshold = 1;
            var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
                maximalAbstentionThreshold, maximalRejectionThreshold, Reviewer1);
            var proposalId = await CreateProposalAsync(Reviewer1KeyPair, organizationAddress);
            var associationContractStub = GetAssociationContractTester(Reviewer1KeyPair);

            var transactionResult1 =
                await associationContractStub.Approve.SendAsync(proposalId);
            transactionResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var transactionResult2 =
                await associationContractStub.Approve.SendWithExceptionAsync(proposalId);
            transactionResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        }

        [Fact]
        public async Task Approve_Proposal_Failed_Test()
        {
            //not found
            {
                var transactionResult =
                    await AssociationContractStub.Approve.SendWithExceptionAsync(Hash.FromString("Test"));
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            }

            var minimalApproveThreshold = 2;
            var minimalVoteThreshold = 3;
            var maximalAbstentionThreshold = 1;
            var maximalRejectionThreshold = 1;
            var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
                maximalAbstentionThreshold, maximalRejectionThreshold, Reviewer1);
            var proposalId = await CreateProposalAsync(Reviewer1KeyPair, organizationAddress);

            //not authorize
            {
                var associationContractStub = GetAssociationContractTester(DefaultSenderKeyPair);
                var transactionResult = await associationContractStub.Approve.SendWithExceptionAsync(proposalId);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            }

            //expired time
            {
                var associationContractStub = GetAssociationContractTester(Reviewer1KeyPair);
                BlockTimeProvider.SetBlockTime(BlockTimeProvider.GetBlockTime().AddDays(5));
                var error = await associationContractStub.Approve.CallWithExceptionAsync(proposalId);
                error.Value.ShouldContain("Invalid proposal.");
            }

            //already exist
            {
                var associationContractStub = GetAssociationContractTester(Reviewer1KeyPair);
                BlockTimeProvider.SetBlockTime(BlockTimeProvider.GetBlockTime().AddDays(-5));
                var transactionResult1 =
                    await associationContractStub.Approve.SendAsync(proposalId);
                transactionResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var transactionResult2 =
                    await associationContractStub.Reject.SendWithExceptionAsync(proposalId);
                transactionResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult2.TransactionResult.Error.Contains("Sender already voted").ShouldBeTrue();

                var transactionResult3 =
                    await associationContractStub.Abstain.SendWithExceptionAsync(proposalId);
                transactionResult3.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult3.TransactionResult.Error.Contains("Sender already voted").ShouldBeTrue();
            }
        }

        [Fact]
        public async Task Check_Proposal_ToBeRelease()
        {
            var minimalApproveThreshold = 1;
            var minimalVoteThreshold = 3;
            var maximalAbstentionThreshold = 1;
            var maximalRejectionThreshold = 1;
            var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
                maximalAbstentionThreshold, maximalRejectionThreshold, Reviewer1);
            //Abstain probability >= maximalAbstentionThreshold
            {
                var proposalId = await CreateProposalAsync(Reviewer1KeyPair, organizationAddress);
                var associationContractStub = GetAssociationContractTester(Reviewer1KeyPair);
                await AbstainAsync(Reviewer2KeyPair, proposalId);
                await AbstainAsync(Reviewer3KeyPair, proposalId);
                await ApproveAsync(Reviewer1KeyPair, proposalId);
                var result = await associationContractStub.GetProposal.CallAsync(proposalId);
                result.ToBeReleased.ShouldBeFalse();
            }
            //Rejection probability > maximalRejectionThreshold
            {
                var proposalId = await CreateProposalAsync(Reviewer1KeyPair, organizationAddress);
                var associationContractStub = GetAssociationContractTester(Reviewer1KeyPair);
                await RejectAsync(Reviewer1KeyPair, proposalId);
                await RejectAsync(Reviewer2KeyPair, proposalId);
                await ApproveAsync(Reviewer3KeyPair, proposalId);
                var result = await associationContractStub.GetProposal.CallAsync(proposalId);
                result.ToBeReleased.ShouldBeFalse();
            }
            //Approve probability > minimalApprovalThreshold && voted count >= minimalVoteThreshold
            {
                var proposalId = await CreateProposalAsync(Reviewer1KeyPair, organizationAddress);
                var associationContractStub = GetAssociationContractTester(Reviewer1KeyPair);
                await AbstainAsync(Reviewer1KeyPair, proposalId);
                await RejectAsync(Reviewer2KeyPair, proposalId);
                await ApproveAsync(Reviewer3KeyPair, proposalId);
                var result = await associationContractStub.GetProposal.CallAsync(proposalId);
                result.ToBeReleased.ShouldBeTrue();
            }
        }

        [Fact]
        public async Task Release_Proposal_Failed_Test()
        {
            //not found
            {
                var fakeId = Hash.FromString("test");
                AssociationContractStub = GetAssociationContractTester(Reviewer2KeyPair);
                var result = await AssociationContractStub.Release.SendWithExceptionAsync(fakeId);
                //Proposal not found
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                result.TransactionResult.Error.Contains("Invalid proposal id.").ShouldBeTrue();
            }

            var minimalApproveThreshold = 2;
            var minimalVoteThreshold = 3;
            var maximalAbstentionThreshold = 1;
            var maximalRejectionThreshold = 1;
            var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
                maximalAbstentionThreshold, maximalRejectionThreshold, Reviewer1);
            var proposalId = await CreateProposalAsync(Reviewer1KeyPair, organizationAddress);
            await TransferToOrganizationAddressAsync(organizationAddress);

            {
                await ApproveAsync(Reviewer1KeyPair, proposalId);
                var associationContractStub = GetAssociationContractTester(Reviewer1KeyPair);
                var result = await associationContractStub.Release.SendWithExceptionAsync(proposalId);
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                result.TransactionResult.Error.Contains("Not approved.").ShouldBeTrue();
            }

            //wrong sender
            {
                await ApproveAsync(Reviewer3KeyPair, proposalId);

                var associationContractStub = GetAssociationContractTester(Reviewer2KeyPair);
                var result = await associationContractStub.Release.SendWithExceptionAsync(proposalId);
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                result.TransactionResult.Error.ShouldContain("No permission.");
            }
        }

        [Fact]
        public async Task Release_Proposal_Success_Test()
        {
            var minimalApproveThreshold = 2;
            var minimalVoteThreshold = 3;
            var maximalAbstentionThreshold = 1;
            var maximalRejectionThreshold = 1;
            var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
                maximalAbstentionThreshold, maximalRejectionThreshold, Reviewer1);
            var proposalId = await CreateProposalAsync(Reviewer1KeyPair, organizationAddress);
            await TransferToOrganizationAddressAsync(organizationAddress);
            await ApproveAsync(Reviewer3KeyPair, proposalId);
            await ApproveAsync(Reviewer2KeyPair, proposalId);
            await ApproveAsync(Reviewer1KeyPair, proposalId);

            var associationContractStub = GetAssociationContractTester(Reviewer1KeyPair);
            var result = await associationContractStub.Release.SendAsync(proposalId);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            // Check inline transaction result
            var getBalance = TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = "ELF",
                Owner = Reviewer1
            }).Result.Balance;
            getBalance.ShouldBe(100);
        }

        [Fact]
        public async Task Release_Proposal_AlreadyReleased_Test()
        {
            var minimalApproveThreshold = 2;
            var minimalVoteThreshold = 3;
            var maximalAbstentionThreshold = 1;
            var maximalRejectionThreshold = 1;
            var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
                maximalAbstentionThreshold, maximalRejectionThreshold, Reviewer1);
            var proposalId = await CreateProposalAsync(Reviewer1KeyPair, organizationAddress);
            await TransferToOrganizationAddressAsync(organizationAddress);
            await ApproveAsync(Reviewer3KeyPair, proposalId);
            await ApproveAsync(Reviewer2KeyPair, proposalId);
            await ApproveAsync(Reviewer1KeyPair, proposalId);

            {
                var associationContractStub = GetAssociationContractTester(Reviewer1KeyPair);
                var result = await associationContractStub.Release.SendAsync(proposalId);
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                var proposalReleased = ProposalReleased.Parser.ParseFrom(result.TransactionResult.Logs
                        .First(l => l.Name.Contains(nameof(ProposalReleased))).NonIndexed)
                    .ProposalId;
                proposalReleased.ShouldBe(proposalId);

                //After release,the proposal will be deleted
                var getProposal = await associationContractStub.GetProposal.CallAsync(proposalId);
                getProposal.ShouldBe(new ProposalOutput());
            }

            //approve the same proposal again
            {
                var associationContractStub1 = GetAssociationContractTester(Reviewer3KeyPair);
                var transactionResult =
                    await associationContractStub1.Approve.SendWithExceptionAsync(proposalId);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.ShouldContain("Invalid proposal");

                //release the same proposal again
                var associationContractStub2 = GetAssociationContractTester(Reviewer2KeyPair);
                var transactionResult2 = await associationContractStub2.Release.SendWithExceptionAsync(proposalId);
                transactionResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult2.TransactionResult.Error.ShouldContain("Invalid proposal id.");
            }
        }

        [Fact]
        public async Task Change_OrganizationThreshold_Test()
        {
            var minimalApproveThreshold = 1;
            var minimalVoteThreshold = 1;
            var maximalAbstentionThreshold = 1;
            var maximalRejectionThreshold = 1;
            var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
                maximalAbstentionThreshold, maximalRejectionThreshold, Reviewer1);
            var proposalId = await CreateProposalAsync(Reviewer1KeyPair, organizationAddress);
            await ApproveAsync(Reviewer1KeyPair, proposalId);
            var proposal = await AssociationContractStub.GetProposal.CallAsync(proposalId);
            proposal.ToBeReleased.ShouldBeTrue();


            {
                var proposalReleaseThresholdInput = new ProposalReleaseThreshold
                {
                    MinimalVoteThreshold = 2
                };

                var associationContractStub = GetAssociationContractTester(Reviewer1KeyPair);
                var changeProposalId = await CreateAssociationProposalAsync(Reviewer1KeyPair,
                    proposalReleaseThresholdInput,
                    nameof(associationContractStub.ChangeOrganizationThreshold), organizationAddress);
                await ApproveAsync(Reviewer1KeyPair, changeProposalId);
                var result = await associationContractStub.Release.SendWithExceptionAsync(changeProposalId);
                result.TransactionResult.Error.ShouldContain("Invalid organization.");
            }

            {
                var proposalReleaseThresholdInput = new ProposalReleaseThreshold
                {
                    MinimalVoteThreshold = 2,
                    MinimalApprovalThreshold = minimalApproveThreshold
                };

                var associationContractStub = GetAssociationContractTester(Reviewer1KeyPair);
                var changeProposalId = await CreateAssociationProposalAsync(Reviewer1KeyPair,
                    proposalReleaseThresholdInput,
                    nameof(associationContractStub.ChangeOrganizationThreshold), organizationAddress);
                await ApproveAsync(Reviewer1KeyPair, changeProposalId);
                var result = await associationContractStub.Release.SendAsync(changeProposalId);
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                proposal = await associationContractStub.GetProposal.CallAsync(proposalId);
                proposal.ToBeReleased.ShouldBeFalse();
            }
        }

        [Fact]
        public async Task Change_OrganizationMember_Test()
        {
            var minimalApproveThreshold = 1;
            var minimalVoteThreshold = 1;
            var maximalAbstentionThreshold = 1;
            var maximalRejectionThreshold = 1;
            var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
                maximalAbstentionThreshold, maximalRejectionThreshold, Reviewer1);

            {
                var organizationMember = new OrganizationMemberList
                {
                    OrganizationMembers = {Reviewer1}
                };

                var associationContractStub = GetAssociationContractTester(Reviewer1KeyPair);
                var changeProposalId = await CreateAssociationProposalAsync(Reviewer1KeyPair, organizationMember,
                    nameof(associationContractStub.ChangeOrganizationMember), organizationAddress);
                await ApproveAsync(Reviewer1KeyPair, changeProposalId);
                var releaseResult = await associationContractStub.Release.SendWithExceptionAsync(changeProposalId);
                releaseResult.TransactionResult.Error.ShouldContain("Invalid organization.");
            }

            {
                var organizationMember = new OrganizationMemberList
                {
                    OrganizationMembers = {Reviewer1, Reviewer2}
                };

                AssociationContractStub = GetAssociationContractTester(Reviewer1KeyPair);
                var changeProposalId = await CreateAssociationProposalAsync(Reviewer1KeyPair, organizationMember,
                    nameof(AssociationContractStub.ChangeOrganizationMember), organizationAddress);
                await ApproveAsync(Reviewer1KeyPair, changeProposalId);
                var releaseResult = await AssociationContractStub.Release.SendAsync(changeProposalId);
                releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var organizationInfo = await AssociationContractStub.GetOrganization.CallAsync(organizationAddress);
                organizationInfo.OrganizationMemberList.ShouldBe(organizationMember);
            }
        }

        [Fact]
        public async Task Change_OrganizationProposalWhitelist_Test()
        {
            var minimalApproveThreshold = 1;
            var minimalVoteThreshold = 1;
            var maximalAbstentionThreshold = 1;
            var maximalRejectionThreshold = 1;
            var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
                maximalAbstentionThreshold, maximalRejectionThreshold, Reviewer1);

            var proposerWhiteList = new ProposerWhiteList
            {
                Proposers = {Reviewer2}
            };

            AssociationContractStub = GetAssociationContractTester(Reviewer1KeyPair);
            var changeProposalId = await CreateAssociationProposalAsync(Reviewer1KeyPair, proposerWhiteList,
                nameof(AssociationContractStub.ChangeOrganizationProposerWhiteList), organizationAddress);
            await ApproveAsync(Reviewer1KeyPair, changeProposalId);
            var releaseResult = await AssociationContractStub.Release.SendAsync(changeProposalId);
            releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            await TransferToOrganizationAddressAsync(organizationAddress);
            var transferInput = new TransferInput()
            {
                Symbol = "ELF",
                Amount = 100,
                To = Reviewer1,
                Memo = "Transfer"
            };
            var associationContractStub = GetAssociationContractTester(Reviewer1KeyPair);
            var createProposalInput = new CreateProposalInput
            {
                ContractMethodName = nameof(TokenContractStub.Approve),
                ToAddress = TokenContractAddress,
                Params = transferInput.ToByteString(),
                ExpiredTime = BlockTimeProvider.GetBlockTime().AddDays(2),
                OrganizationAddress = organizationAddress
            };
            var result = await associationContractStub.CreateProposal.SendWithExceptionAsync(createProposalInput);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.ShouldContain("Unauthorized to propose.");
        }

        private async Task<Hash> CreateProposalAsync(ECKeyPair proposalKeyPair, Address organizationAddress)
        {
            var transferInput = new TransferInput()
            {
                Symbol = "ELF",
                Amount = 100,
                To = Reviewer1,
                Memo = "Transfer"
            };
            var associationContractStub = GetAssociationContractTester(proposalKeyPair);
            var createProposalInput = new CreateProposalInput
            {
                ContractMethodName = nameof(TokenContractStub.Transfer),
                ToAddress = TokenContractAddress,
                Params = transferInput.ToByteString(),
                ExpiredTime = BlockTimeProvider.GetBlockTime().AddDays(2),
                OrganizationAddress = organizationAddress
            };
            var proposal = await associationContractStub.CreateProposal.SendAsync(createProposalInput);
            var proposalCreated = ProposalCreated.Parser.ParseFrom(proposal.TransactionResult.Logs
                    .First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
                .ProposalId;
            proposal.Output.ShouldBe(proposalCreated);

            return proposal.Output;
        }

        private async Task<Hash> CreateAssociationProposalAsync(ECKeyPair proposalKeyPair, IMessage input,
            string method, Address organizationAddress)
        {
            var associationContractStub = GetAssociationContractTester(proposalKeyPair);
            var createProposalInput = new CreateProposalInput
            {
                ContractMethodName = method,
                ToAddress = AssociationContractAddress,
                Params = input.ToByteString(),
                ExpiredTime = BlockTimeProvider.GetBlockTime().AddDays(2),
                OrganizationAddress = organizationAddress
            };
            var proposal = await associationContractStub.CreateProposal.SendAsync(createProposalInput);
            var proposalCreated = ProposalCreated.Parser.ParseFrom(proposal.TransactionResult.Logs
                    .First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
                .ProposalId;
            proposal.Output.ShouldBe(proposalCreated);

            return proposal.Output;
        }

        private async Task<Address> CreateOrganizationAsync(int minimalApproveThreshold, int minimalVoteThreshold,
            int maximalAbstentionThreshold, int maximalRejectionThreshold, params Address[] proposers)
        {
            var createOrganizationInput = GenerateCreateOrganizationInput(minimalApproveThreshold, minimalVoteThreshold,
                maximalAbstentionThreshold, maximalRejectionThreshold, proposers);
            var transactionResult =
                await AssociationContractStub.CreateOrganization.SendAsync(createOrganizationInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            return transactionResult.Output;
        }

        private CreateOrganizationInput GenerateCreateOrganizationInput(int minimalApproveThreshold,
            int minimalVoteThreshold,
            int maximalAbstentionThreshold, int maximalRejectionThreshold, params Address[] proposers)
        {
            var createOrganizationInput = new CreateOrganizationInput
            {
                OrganizationMemberList = new OrganizationMemberList
                {
                    OrganizationMembers = {Reviewer1, Reviewer2, Reviewer3}
                },
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MinimalApprovalThreshold = minimalApproveThreshold,
                    MinimalVoteThreshold = minimalVoteThreshold,
                    MaximalAbstentionThreshold = maximalAbstentionThreshold,
                    MaximalRejectionThreshold = maximalRejectionThreshold
                },
                ProposerWhiteList = new ProposerWhiteList
                {
                    Proposers = {proposers}
                }
            };
            return createOrganizationInput;
        }

        private async Task TransferToOrganizationAddressAsync(Address organizationAddress)
        {
            await TokenContractStub.Transfer.SendAsync(new TransferInput
            {
                Symbol = "ELF",
                Amount = 200,
                To = organizationAddress,
                Memo = "transfer organization address"
            });
        }

        private async Task ApproveAsync(ECKeyPair reviewer, Hash proposalId)
        {
            var associationContractStub = GetAssociationContractTester(reviewer);

            var transactionResult1 =
                await associationContractStub.Approve.SendAsync(proposalId);
            transactionResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        private async Task RejectAsync(ECKeyPair reviewer, Hash proposalId)
        {
            var associationContractStub = GetAssociationContractTester(reviewer);

            var transactionResult1 =
                await associationContractStub.Reject.SendAsync(proposalId);
            transactionResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        private async Task AbstainAsync(ECKeyPair reviewer, Hash proposalId)
        {
            var associationContractStub = GetAssociationContractTester(reviewer);

            var transactionResult1 =
                await associationContractStub.Abstain.SendAsync(proposalId);
            transactionResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
    }
}