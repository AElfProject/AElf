using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.ContractTestBase.ContractTestKit;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core.Extension;
using AElf.GovernmentSystem;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Standards.ACS1;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Association;

public class AssociationContractTests : AssociationContractTestBase<AssociationContractTestAElfModule>
{
    private readonly IBlockchainService _blockchainService;
    private readonly TestDemoSmartContractAddressNameProvider _smartContractAddressNameProvider;
    private readonly ISmartContractAddressService _smartContractAddressService;

    public AssociationContractTests()
    {
        _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
        _blockchainService = GetRequiredService<IBlockchainService>();
        _smartContractAddressNameProvider = GetRequiredService<TestDemoSmartContractAddressNameProvider>();
    }

    [Fact]
    public async Task Get_Organization_Test()
    {
        //failed case
        {
            var organization =
                await AssociationContractStub.GetOrganization.CallAsync(Accounts[0].Address);
            organization.ShouldBe(new Organization());
        }
    }

    [Fact]
    public async Task CreateOrganization_Success_Test()
    {
        var minimalApproveThreshold = 2;
        var minimalVoteThreshold = 3;
        var maximalAbstentionThreshold = 1;
        var maximalRejectionThreshold = 1;
        var createOrganizationInput = new CreateOrganizationInput
        {
            OrganizationMemberList = new OrganizationMemberList
            {
                OrganizationMembers = { Reviewer1, Reviewer2, Reviewer3 }
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
                Proposers = { Reviewer1 }
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
        getOrganization.OrganizationHash.ShouldBe(HashHelper.ComputeFrom(createOrganizationInput));
    }

    [Fact]
    public async Task Create_OrganizationOnMultiChains_Test()
    {
        //failed case
        {
            var organization =
                await AssociationContractStub.GetOrganization.CallAsync(Accounts[0].Address);
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
                    OrganizationMembers = { Reviewer1, Reviewer2, Reviewer3 }
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
                    Proposers = { Reviewer1 }
                }
            };
            var transactionResult =
                await AssociationContractStub.CreateOrganization.SendAsync(createOrganizationInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var organizationAddress = transactionResult.Output;

            var testKit = CreateContractTestKit<AssociationContractTestAElfModule>(new ChainInitializationDto
            {
                ChainId = 1
            });
            SystemContractAddresses[AssociationSmartContractAddressNameProvider.Name]
                .ShouldNotBe(testKit.SystemContractAddresses[AssociationSmartContractAddressNameProvider.Name]);
            var otherChainAssociationContractStub =
                testKit.GetTester<AssociationContractImplContainer.AssociationContractImplStub>(
                    testKit.SystemContractAddresses[AssociationSmartContractAddressNameProvider.Name],
                    DefaultSenderKeyPair);
            var executionResult =
                await otherChainAssociationContractStub.CreateOrganization.SendAsync(createOrganizationInput);
            var organizationAddressOnAnotherChain = executionResult.Output;
            organizationAddressOnAnotherChain.ShouldBe(organizationAddress);
        }
    }

    [Fact]
    public async Task Get_Proposal_Test()
    {
        //failed case
        {
            var proposal = await AssociationContractStub.GetProposal.CallAsync(HashHelper.ComputeFrom("Test"));
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
            var transferInput = new TransferInput
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
            getProposal.Output.Title.ShouldNotBeNullOrEmpty();
            getProposal.Output.Description.ShouldNotBeNullOrEmpty();
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
            ToAddress = Accounts[0].Address,
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
            createProposalInput.ToAddress = Accounts[0].Address;

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
            createProposalInput.OrganizationAddress = Accounts[1].Address;

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

        // "Proposal with invalid url"
        {
            var anotherAssociationContractStub = GetAssociationContractTester(DefaultSenderKeyPair);
            createProposalInput.ProposalDescriptionUrl = "test.com";
            var transactionResult =
                await anotherAssociationContractStub.CreateProposal.SendWithExceptionAsync(createProposalInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);

            createProposalInput.ProposalDescriptionUrl = "https://test.com/test%abcd%&wxyz";
            var transactionResult2 =
                await associationContractStub.CreateProposal.SendAsync(createProposalInput);
            transactionResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
    }

    [Fact]
    public async Task Approve_Proposal_NotFoundProposal_Test()
    {
        var transactionResult =
            await AssociationContractStub.Approve.SendWithExceptionAsync(HashHelper.ComputeFrom("Test"));
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
    public async Task ClearProposal_Fail_Test()
    {
        // proposal does not exist
        {
            var ret = await AssociationContractStub.ClearProposal.SendWithExceptionAsync(new Hash());
            ret.TransactionResult.Error.ShouldContain("Proposal clear failed");
        }

        //proposal does not expire
        {
            var minimalApproveThreshold = 2;
            var minimalVoteThreshold = 3;
            var maximalAbstentionThreshold = 1;
            var maximalRejectionThreshold = 1;
            var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
                maximalAbstentionThreshold, maximalRejectionThreshold, DefaultSender);
            var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
            var ret = await AssociationContractStub.ClearProposal.SendWithExceptionAsync(proposalId);
            ret.TransactionResult.Error.ShouldContain("Proposal clear failed");
        }
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
        var associationContractStub = GetAssociationContractTester(Reviewer1KeyPair);
        BlockTimeProvider.SetBlockTime(BlockTimeProvider.GetBlockTime().AddDays(5));
        var error = await associationContractStub.Approve.CallWithExceptionAsync(proposalId);
        error.Value.ShouldContain("Invalid proposal.");

        //Clear expire proposal
        var result = await associationContractStub.ClearProposal.SendAsync(proposalId);
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var queryProposal = await AssociationContractStub.GetProposal.CallAsync(proposalId);
        queryProposal.ShouldBe(new ProposalOutput());
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
                await AssociationContractStub.Approve.SendWithExceptionAsync(HashHelper.ComputeFrom("Test"));
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
            var fakeId = HashHelper.ComputeFrom("test");
            var associationContractStub = GetAssociationContractTester(Reviewer2KeyPair);
            var result = await associationContractStub.Release.SendWithExceptionAsync(fakeId);
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
    public async Task ChangeOrganizationThreshold_With_Invalid_Sender_Test()
    {
        var ret =
            await AssociationContractStub.ChangeOrganizationThreshold.SendWithExceptionAsync(
                new ProposalReleaseThreshold());
        ret.TransactionResult.Error.ShouldContain("Organization not found");
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
            Proposers = { Reviewer2 }
        };

        var associationContractStub = GetAssociationContractTester(Reviewer1KeyPair);
        var changeProposalId = await CreateAssociationProposalAsync(Reviewer1KeyPair, proposerWhiteList,
            nameof(associationContractStub.ChangeOrganizationProposerWhiteList), organizationAddress);
        await ApproveAsync(Reviewer1KeyPair, changeProposalId);
        var releaseResult = await associationContractStub.Release.SendAsync(changeProposalId);
        releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        await TransferToOrganizationAddressAsync(organizationAddress);
        var transferInput = new TransferInput
        {
            Symbol = "ELF",
            Amount = 100,
            To = Reviewer1,
            Memo = "Transfer"
        };
        associationContractStub = GetAssociationContractTester(Reviewer1KeyPair);
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

        //Verify association proposal
        var verifyResult = await associationContractStub.ValidateProposerInWhiteList.CallAsync(
            new ValidateProposerInWhiteListInput
            {
                OrganizationAddress = organizationAddress,
                Proposer = Reviewer2
            });
        verifyResult.Value.ShouldBeTrue();
    }

    [Fact]
    public async Task ChangeMethodFeeController_With_Invalid_Organization_Test()
    {
        var methodFeeController = await AssociationContractStub.GetMethodFeeController.CallAsync(new Empty());
        var defaultOrganization = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        methodFeeController.OwnerAddress.ShouldBe(defaultOrganization);

        const string proposalCreationMethodName = nameof(AssociationContractStub.ChangeMethodFeeController);

        var proposalId = await CreateFeeProposalAsync(AssociationContractAddress,
            methodFeeController.OwnerAddress, proposalCreationMethodName, new AuthorityInfo
            {
                OwnerAddress = ParliamentContractAddress,
                ContractAddress = ParliamentContractAddress
            });

        await ApproveWithMinersAsync(proposalId);
        var releaseResult = await ParliamentContractStub.Release.SendWithExceptionAsync(proposalId);
        releaseResult.TransactionResult.Error.ShouldContain("Invalid authority input");
    }

    [Fact]
    public async Task ChangeMethodFeeController_Test()
    {
        var createOrganizationResult =
            await ParliamentContractStub.CreateOrganization.SendAsync(
                new Parliament.CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MinimalApprovalThreshold = 1000,
                        MinimalVoteThreshold = 1000
                    }
                });
        var organizationAddress = Address.Parser.ParseFrom(createOrganizationResult.TransactionResult.ReturnValue);

        var methodFeeController = await AssociationContractStub.GetMethodFeeController.CallAsync(new Empty());
        var defaultOrganization = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        methodFeeController.OwnerAddress.ShouldBe(defaultOrganization);

        const string proposalCreationMethodName = nameof(AssociationContractStub.ChangeMethodFeeController);

        var proposalId = await CreateFeeProposalAsync(AssociationContractAddress,
            methodFeeController.OwnerAddress, proposalCreationMethodName, new AuthorityInfo
            {
                OwnerAddress = organizationAddress,
                ContractAddress = ParliamentContractAddress
            });

        await ApproveWithMinersAsync(proposalId);
        var releaseResult = await ParliamentContractStub.Release.SendAsync(proposalId);
        releaseResult.TransactionResult.Error.ShouldBeNullOrEmpty();
        releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var newMethodFeeController = await AssociationContractStub.GetMethodFeeController.CallAsync(new Empty());
        Assert.True(newMethodFeeController.OwnerAddress == organizationAddress);
    }

    [Fact]
    public async Task ChangeMethodFeeController_WithoutAuth_Test()
    {
        var createOrganizationResult =
            await ParliamentContractStub.CreateOrganization.SendAsync(
                new Parliament.CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MinimalApprovalThreshold = 1000,
                        MinimalVoteThreshold = 1000
                    }
                });
        var organizationAddress = Address.Parser.ParseFrom(createOrganizationResult.TransactionResult.ReturnValue);
        var result = await AssociationContractStub.ChangeMethodFeeController.SendWithExceptionAsync(
            new AuthorityInfo
            {
                OwnerAddress = organizationAddress,
                ContractAddress = ParliamentContractAddress
            });

        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        result.TransactionResult.Error.Contains("Unauthorized behavior.").ShouldBeTrue();
    }

    [Fact]
    public async Task SetMethodFee_Fail_Test()
    {
        // fee < 0
        {
            var invalidMethodFees = GetValidMethodFees();
            invalidMethodFees.Fees[0].BasicFee = -1;
            var ret = await AssociationContractStub.SetMethodFee.SendWithExceptionAsync(invalidMethodFees);
            ret.TransactionResult.Error.ShouldContain("Invalid amount");
        }

        // token does not exist
        {
            var invalidMethodFees = GetValidMethodFees();
            invalidMethodFees.Fees[0].Symbol = "NOTEXIST";
            var ret = await AssociationContractStub.SetMethodFee.SendWithExceptionAsync(invalidMethodFees);
            ret.TransactionResult.Error.ShouldContain("Token is not found");
        }

        // token is not profitable
        {
            var tokenSymbol = "DLS";
            var invalidMethodFees = GetValidMethodFees();
            invalidMethodFees.Fees[0].Symbol = tokenSymbol;
            
            var defaultOrganization = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());

            var proposalId = await CreateFeeProposalAsync(TokenContractAddress,
                defaultOrganization, nameof(TokenContractStub.Create), new CreateInput
                {
                    Symbol = tokenSymbol,
                    TokenName = "name",
                    Issuer = DefaultSender,
                    TotalSupply = 1000_000,
                    Owner = DefaultSender,
                });

            await ApproveWithMinersAsync(proposalId);
            await ParliamentContractStub.Release.SendAsync(proposalId);
            
            var ret = await AssociationContractStub.SetMethodFee.SendWithExceptionAsync(invalidMethodFees);
            ret.TransactionResult.Error.ShouldContain($"Token {tokenSymbol} cannot set as method fee.");
        }

        // without authority
        {
            var invalidMethodFees = GetValidMethodFees();
            var ret = await AssociationContractStub.SetMethodFee.SendWithExceptionAsync(invalidMethodFees);
            ret.TransactionResult.Error.ShouldContain("Unauthorized to set method fee");
        }
    }

    [Fact]
    public async Task SetMethodFee_Test()
    {
        var input = new MethodFees
        {
            MethodName = nameof(AssociationContractStub.CreateOrganization),
            Fees =
            {
                new MethodFee
                {
                    Symbol = "ELF",
                    BasicFee = 5000_0000L
                }
            }
        };
        var defaultOrganization = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        var proposal = await ParliamentContractStub.CreateProposal.SendAsync(new CreateProposalInput
        {
            ContractMethodName = nameof(AssociationContractStub.SetMethodFee),
            ToAddress = AssociationContractAddress,
            Params = input.ToByteString(),
            ExpiredTime = BlockTimeProvider.GetBlockTime().AddDays(1),
            OrganizationAddress = defaultOrganization
        });
        var proposalId = proposal.Output;
        await ApproveWithMinersAsync(proposalId);
        await ParliamentContractStub.Release.SendAsync(proposalId);

        //Query result
        var transactionFee = await AssociationContractStub.GetMethodFee.CallAsync(new StringValue
        {
            Value = nameof(AssociationContractStub.CreateOrganization)
        });
        var feeItem = transactionFee.Fees.First();
        feeItem.Symbol.ShouldBe("ELF");
        feeItem.BasicFee.ShouldBe(5000_0000L);
    }

    [Fact]
    public async Task CreateOrganizationBySystemContract_Test()
    {
        var minimalApproveThreshold = 2;
        var minimalVoteThreshold = 3;
        var maximalAbstentionThreshold = 1;
        var maximalRejectionThreshold = 1;
        var createOrganizationInput = new CreateOrganizationInput
        {
            OrganizationMemberList = new OrganizationMemberList
            {
                OrganizationMembers = { DefaultSender, Reviewer1, Reviewer2, Reviewer3 }
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
                Proposers = { DefaultSender }
            }
        };
        var input = new CreateOrganizationBySystemContractInput
        {
            OrganizationCreationInput = createOrganizationInput,
            OrganizationAddressFeedbackMethod = ""
        };
        //Unauthorized to create organization
        var addressByCalculate =
            await AssociationContractStub.CalculateOrganizationAddress.SendAsync(createOrganizationInput);
        var transactionResult =
            await AssociationContractStub.CreateOrganizationBySystemContract.SendWithExceptionAsync(input);
        transactionResult.TransactionResult.Error.ShouldContain("Unauthorized");
        //success
        var chain = _blockchainService.GetChainAsync();
        var blockIndex = new BlockIndex
        {
            BlockHash = chain.Result.BestChainHash,
            BlockHeight = chain.Result.BestChainHeight
        };
        await _smartContractAddressService.SetSmartContractAddressAsync(blockIndex,
            _smartContractAddressNameProvider.ContractStringName, DefaultSender);

        var transactionResult1 =
            await AssociationContractStub.CreateOrganizationBySystemContract.SendAsync(input);
        transactionResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        transactionResult1.Output.ShouldBe(addressByCalculate.Output);
        var boolResult =
            await AssociationContractStub.ValidateOrganizationExist.SendAsync(addressByCalculate.Output);
        boolResult.Output.Value.ShouldBeTrue();
        //invalid contract
        var method = "OrganizationAddressFeedbackMethodName";
        var input2 = new CreateOrganizationBySystemContractInput
        {
            OrganizationCreationInput = createOrganizationInput,
            OrganizationAddressFeedbackMethod = method
        };
        var transactionResult2 =
            await AssociationContractStub.CreateOrganizationBySystemContract.SendWithExceptionAsync(input2);
        transactionResult2.TransactionResult.Error.ShouldContain("invalid");
    }

    [Fact]
    public async Task CreateProposalBySystemContract_With_Invalid_Organization_Test()
    {
        var input = new CreateProposalBySystemContractInput
        {
            OriginProposer = DefaultSender
        };
        var chain = _blockchainService.GetChainAsync();
        var blockIndex = new BlockIndex
        {
            BlockHash = chain.Result.BestChainHash,
            BlockHeight = chain.Result.BestChainHeight
        };
        await _smartContractAddressService.SetSmartContractAddressAsync(blockIndex,
            _smartContractAddressNameProvider.ContractStringName, DefaultSender);
        // invalid organization address
        {
            var proposalInput = GetValidProposalInput(AssociationContractAddress);
            input.ProposalInput = proposalInput;
            var ret =
                await AssociationContractStub.CreateProposalBySystemContract.SendWithExceptionAsync(input);
            ret.TransactionResult.Error.ShouldContain("No registered organization");
        }

        //invalid organization proposer
        {
            var proposer = Address.FromPublicKey(Accounts[2].KeyPair.PublicKey);
            var minimalApproveThreshold = 2;
            var minimalVoteThreshold = 3;
            var maximalAbstentionThreshold = 1;
            var maximalRejectionThreshold = 1;
            var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
                maximalAbstentionThreshold, maximalRejectionThreshold, proposer);
            var proposalInput = GetValidProposalInput(organizationAddress);
            input.ProposalInput = proposalInput;
            var ret =
                await AssociationContractStub.CreateProposalBySystemContract.SendWithExceptionAsync(input);
            ret.TransactionResult.Error.ShouldContain("Unauthorized to propose");
        }
    }

    [Fact]
    public async Task CreateProposalBySystemContract_With_Invalid_Proposal_Test()
    {
        var input = new CreateProposalBySystemContractInput
        {
            OriginProposer = DefaultSender
        };
        var chain = _blockchainService.GetChainAsync();
        var blockIndex = new BlockIndex
        {
            BlockHash = chain.Result.BestChainHash,
            BlockHeight = chain.Result.BestChainHeight
        };
        await _smartContractAddressService.SetSmartContractAddressAsync(blockIndex,
            _smartContractAddressNameProvider.ContractStringName, DefaultSender);
        var minimalApproveThreshold = 2;
        var minimalVoteThreshold = 3;
        var maximalAbstentionThreshold = 1;
        var maximalRejectionThreshold = 1;
        var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, DefaultSender);

        // invalid contract method name
        {
            var proposalInput = GetValidProposalInput(organizationAddress);
            proposalInput.ContractMethodName = string.Empty;
            input.ProposalInput = proposalInput;
            var ret =
                await AssociationContractStub.CreateProposalBySystemContract.SendWithExceptionAsync(input);
            ret.TransactionResult.Error.ShouldContain("Invalid proposal");
        }

        // invalid expire time
        {
            var proposalInput = GetValidProposalInput(organizationAddress);
            proposalInput.ExpiredTime = new Timestamp();
            input.ProposalInput = proposalInput;
            var ret =
                await AssociationContractStub.CreateProposalBySystemContract.SendWithExceptionAsync(input);
            ret.TransactionResult.Error.ShouldContain("Invalid proposal");
        }

        // invalid url
        {
            var proposalInput = GetValidProposalInput(organizationAddress);
            proposalInput.ProposalDescriptionUrl = "TPP.og";
            input.ProposalInput = proposalInput;
            var ret =
                await AssociationContractStub.CreateProposalBySystemContract.SendWithExceptionAsync(input);
            ret.TransactionResult.Error.ShouldContain("Invalid proposal");
        }
    }

    [Fact]
    public async Task CreateProposalBySystemContract_Test()
    {
        var minimalApproveThreshold = 2;
        var minimalVoteThreshold = 3;
        var maximalAbstentionThreshold = 1;
        var maximalRejectionThreshold = 1;
        var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, DefaultSender);

        var transferInput = new TransferInput
        {
            Symbol = "ELF",
            Amount = 100,
            To = Reviewer1,
            Memo = "Transfer"
        };
        var createProposalInput = new CreateProposalInput
        {
            ContractMethodName = nameof(TokenContractStub.Transfer),
            ToAddress = TokenContractAddress,
            Params = transferInput.ToByteString(),
            ExpiredTime = BlockTimeProvider.GetBlockTime().AddDays(2),
            OrganizationAddress = organizationAddress
        };
        var input = new CreateProposalBySystemContractInput
        {
            ProposalInput = createProposalInput, OriginProposer = DefaultSender
        };
        var chain = _blockchainService.GetChainAsync();
        var blockIndex = new BlockIndex
        {
            BlockHash = chain.Result.BestChainHash,
            BlockHeight = chain.Result.BestChainHeight
        };
        //Unauthorized to propose
        var transactionResult =
            await AssociationContractStub.CreateProposalBySystemContract.SendWithExceptionAsync(input);
        transactionResult.TransactionResult.Error.ShouldContain("Not authorized to propose");
        //success
        await _smartContractAddressService.SetSmartContractAddressAsync(blockIndex,
            _smartContractAddressNameProvider.ContractStringName, DefaultSender);
        var transactionResult2 =
            await AssociationContractStub.CreateProposalBySystemContract.SendAsync(input);
        transactionResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }

    private async Task<Hash> CreateProposalAsync(ECKeyPair proposalKeyPair, Address organizationAddress)
    {
        var transferInput = new TransferInput
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
            OrganizationAddress = organizationAddress,
            Title = "Token Transfer",
            Description = "Transfer 100 ELF to Reviewer1's address",
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
                OrganizationMembers = { Reviewer1, Reviewer2, Reviewer3 }
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
                Proposers = { proposers }
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
        var utcNow = TimestampHelper.GetUtcNow();
        BlockTimeProvider.SetBlockTime(utcNow);
        var transactionResult =
            await associationContractStub.Approve.SendAsync(proposalId);
        transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var receiptCreated = ReceiptCreated.Parser.ParseFrom(transactionResult.TransactionResult.Logs
            .FirstOrDefault(l => l.Name == nameof(ReceiptCreated))
            ?.NonIndexed);
        ValidateReceiptCreated(receiptCreated, Address.FromPublicKey(reviewer.PublicKey), proposalId, utcNow,
            nameof(associationContractStub.Approve));
    }

    private async Task RejectAsync(ECKeyPair reviewer, Hash proposalId)
    {
        var associationContractStub = GetAssociationContractTester(reviewer);
        var utcNow = TimestampHelper.GetUtcNow();
        BlockTimeProvider.SetBlockTime(utcNow);
        var transactionResult =
            await associationContractStub.Reject.SendAsync(proposalId);
        transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var receiptCreated = ReceiptCreated.Parser.ParseFrom(transactionResult.TransactionResult.Logs
            .FirstOrDefault(l => l.Name == nameof(ReceiptCreated))
            ?.NonIndexed);
        ValidateReceiptCreated(receiptCreated, Address.FromPublicKey(reviewer.PublicKey), proposalId, utcNow,
            nameof(associationContractStub.Reject));
    }

    private async Task AbstainAsync(ECKeyPair reviewer, Hash proposalId)
    {
        var associationContractStub = GetAssociationContractTester(reviewer);

        var utcNow = TimestampHelper.GetUtcNow();
        BlockTimeProvider.SetBlockTime(utcNow);
        var transactionResult =
            await associationContractStub.Abstain.SendAsync(proposalId);
        transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var receiptCreated = ReceiptCreated.Parser.ParseFrom(transactionResult.TransactionResult.Logs
            .FirstOrDefault(l => l.Name == nameof(ReceiptCreated))
            ?.NonIndexed);
        ValidateReceiptCreated(receiptCreated, Address.FromPublicKey(reviewer.PublicKey), proposalId, utcNow,
            nameof(associationContractStub.Abstain));
    }

    private void ValidateReceiptCreated(ReceiptCreated receiptCreated, Address sender, Hash proposalId,
        Timestamp blockTime, string receiptType)
    {
        receiptCreated.Address.ShouldBe(sender);
        receiptCreated.ProposalId.ShouldBe(proposalId);
        receiptCreated.Time.ShouldBe(blockTime);
        receiptCreated.ReceiptType.ShouldBe(receiptType);
    }

    protected async Task<Hash> CreateFeeProposalAsync(Address contractAddress, Address organizationAddress,
        string methodName, IMessage input)
    {
        var proposal = new CreateProposalInput
        {
            OrganizationAddress = organizationAddress,
            ContractMethodName = methodName,
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1),
            Params = input.ToByteString(),
            ToAddress = contractAddress
        };

        var createResult = await ParliamentContractStub.CreateProposal.SendAsync(proposal);
        var proposalId = createResult.Output;
        return proposalId;
    }

    protected async Task ApproveWithMinersAsync(Hash proposalId)
    {
        foreach (var bp in InitialCoreDataCenterKeyPairs)
        {
            var tester = GetParliamentContractTester(bp);
            var approveResult = await tester.Approve.SendAsync(proposalId);
            approveResult.TransactionResult.Error.ShouldBeNullOrEmpty();
        }
    }

    private CreateProposalInput GetValidProposalInput(Address organizationAddress)
    {
        var transferInput = new TransferInput
        {
            Symbol = "ELF",
            Amount = 100,
            To = Reviewer1,
            Memo = "Transfer"
        };
        return new CreateProposalInput
        {
            ContractMethodName = nameof(TokenContractStub.Transfer),
            ToAddress = TokenContractAddress,
            Params = transferInput.ToByteString(),
            ExpiredTime = BlockTimeProvider.GetBlockTime().AddDays(2),
            OrganizationAddress = organizationAddress
        };
    }

    private MethodFees GetValidMethodFees()
    {
        return new MethodFees
        {
            MethodName = "Test",
            Fees =
            {
                new MethodFee
                {
                    Symbol = "ELF",
                    BasicFee = 10
                }
            }
        };
    }
}