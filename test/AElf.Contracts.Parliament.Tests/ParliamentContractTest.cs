using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Standards.ACS1;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Parliament;

public class ParliamentContractTest : ParliamentContractTestBase
{
    private readonly IBlockchainService _blockchainService;
    private readonly ISmartContractAddressNameProvider _smartContractAddressNameProvider;
    private readonly ISmartContractAddressService _smartContractAddressService;

    public ParliamentContractTest()
    {
        _blockchainService = GetRequiredService<IBlockchainService>();
        _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
        _smartContractAddressNameProvider = GetRequiredService<ISmartContractAddressNameProvider>();
        InitializeContracts();
    }

    [Fact]
    public async Task Get_DefaultOrganizationAddress_Test()
    {
        var transactionResult =
            await ParliamentContractStub.GetDefaultOrganizationAddress.SendWithExceptionAsync(new Empty());
        transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        transactionResult.TransactionResult.Error.Contains("Not initialized.").ShouldBeTrue();

        await InitializeParliamentContracts();
        var defaultParliamentAddress =
            await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        defaultParliamentAddress.ShouldNotBeNull();
    }

    [Fact]
    public async Task ParliamentContract_Initialize_Test()
    {
        var result = await ParliamentContractStub.Initialize.SendAsync(new InitializeInput());
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }

    [Fact]
    public async Task ParliamentContract_InitializeTwice_Test()
    {
        await ParliamentContract_Initialize_Test();

        var result = await ParliamentContractStub.Initialize.SendWithExceptionAsync(new InitializeInput());
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        result.TransactionResult.Error.Contains("Already initialized.").ShouldBeTrue();
    }

    [Fact]
    public async Task Get_Organization_Test()
    {
        await InitializeParliamentContracts();
        var minimalApprovalThreshold = 10000 / MinersCount;
        var maximalAbstentionThreshold = 2000 / MinersCount;
        var maximalRejectionThreshold = 3000 / MinersCount;
        var minimalVoteThreshold = 11000 / MinersCount;

        var createOrganizationInput = new CreateOrganizationInput
        {
            ProposalReleaseThreshold = new ProposalReleaseThreshold
            {
                MinimalApprovalThreshold = minimalApprovalThreshold,
                MaximalAbstentionThreshold = maximalAbstentionThreshold,
                MaximalRejectionThreshold = maximalRejectionThreshold,
                MinimalVoteThreshold = minimalVoteThreshold
            }
        };
        var minerParliamentContractStub = GetParliamentContractTester(InitialMinersKeyPairs.First());
        var transactionResult =
            await minerParliamentContractStub.CreateOrganization.SendAsync(createOrganizationInput);
        var organizationCalculated = ParliamentContractStub.CalculateOrganizationAddress
            .CallAsync(createOrganizationInput).Result;
        var organizationAddress = transactionResult.Output;
        organizationCalculated.ShouldBe(organizationAddress);
        var getOrganization = await ParliamentContractStub.GetOrganization.CallAsync(organizationAddress);

        getOrganization.OrganizationAddress.ShouldBe(organizationAddress);
        getOrganization.ProposalReleaseThreshold.MinimalApprovalThreshold.ShouldBe(minimalApprovalThreshold);
        getOrganization.ProposalReleaseThreshold.MinimalVoteThreshold.ShouldBe(minimalVoteThreshold);
        getOrganization.ProposalReleaseThreshold.MaximalAbstentionThreshold.ShouldBe(maximalAbstentionThreshold);
        getOrganization.ProposalReleaseThreshold.MaximalRejectionThreshold.ShouldBe(maximalRejectionThreshold);
        getOrganization.OrganizationHash.ShouldBe(HashHelper.ComputeFrom(createOrganizationInput));
    }

    [Fact]
    public async Task Get_OrganizationFailed_Test()
    {
        var organization =
            await ParliamentContractStub.GetOrganization.CallAsync(Accounts[0].Address);
        organization.ShouldBe(new Organization());
    }

    [Fact]
    public async Task Get_Proposal_Test()
    {
        await InitializeParliamentContracts();
        var minimalApprovalThreshold = 6667;
        var maximalAbstentionThreshold = 2000;
        var maximalRejectionThreshold = 3000;
        var minimalVoteThreshold = 8000;
        var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);
        var transferInput = new TransferInput
        {
            Symbol = "ELF",
            Amount = 100,
            To = Tester
        };
        var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
        var getProposal = await ParliamentContractStub.GetProposal.SendAsync(proposalId);

        getProposal.Output.Proposer.ShouldBe(DefaultSender);
        getProposal.Output.ContractMethodName.ShouldBe(nameof(TokenContractStub.Transfer));
        getProposal.Output.ProposalId.ShouldBe(proposalId);
        getProposal.Output.OrganizationAddress.ShouldBe(organizationAddress);
        getProposal.Output.ToAddress.ShouldBe(TokenContractAddress);

        var transferParam = TransferInput.Parser.ParseFrom(getProposal.Output.Params);
        transferParam.Symbol.ShouldBe(transferInput.Symbol);
        transferParam.Amount.ShouldBe(transferInput.Amount);
        transferParam.To.ShouldBe(transferInput.To);
    }

    [Fact]
    public async Task ApproveMultiProposals_Without_Authority_Test()
    {
        var invalidSenderStub =
            GetTester<ParliamentContractImplContainer.ParliamentContractImplStub>(ParliamentContractAddress,
                TesterKeyPair);
        var approveRet =
            await invalidSenderStub.ApproveMultiProposals.SendWithExceptionAsync(new ProposalIdList());
        approveRet.TransactionResult.Error.ShouldContain("No permission");
    }

    [Fact]
    public async Task ApproveMultiProposals_Test()
    {
        await InitializeParliamentContracts();
        var minimalApprovalThreshold = 6667;
        var maximalAbstentionThreshold = 2000;
        var maximalRejectionThreshold = 3000;
        var minimalVoteThreshold = 8000;
        var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);
        var proposalId1 = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
        var proposalId2 = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);

        ParliamentContractStub = GetParliamentContractTester(InitialMinersKeyPairs[0]);
        var transactionResult =
            await ParliamentContractStub.ApproveMultiProposals.SendAsync(new ProposalIdList
            {
                ProposalIds = { proposalId1, proposalId2 }
            });
        transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var proposal1 = await ParliamentContractStub.GetProposal.CallAsync(proposalId1);
        proposal1.ApprovalCount.ShouldBe(1);
        var proposal2 = await ParliamentContractStub.GetProposal.CallAsync(proposalId1);
        proposal2.ApprovalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Get_ProposalFailed_Test()
    {
        var proposalOutput = await ParliamentContractStub.GetProposal.CallAsync(HashHelper.ComputeFrom("Test"));
        proposalOutput.ShouldBe(new ProposalOutput());
    }

    [Fact]
    public async Task Create_OrganizationFailed_Test()
    {
        await InitializeParliamentContracts();
        var minimalApprovalThreshold = 6667;
        var maximalAbstentionThreshold = 2000;
        var maximalRejectionThreshold = 3000;
        var minimalVoteThreshold = 8000;
        var proposalReleaseThreshold = new ProposalReleaseThreshold
        {
            MinimalApprovalThreshold = minimalApprovalThreshold,
            MaximalAbstentionThreshold = maximalAbstentionThreshold,
            MaximalRejectionThreshold = maximalRejectionThreshold,
            MinimalVoteThreshold = minimalVoteThreshold
        };

        var createOrganizationInput = new CreateOrganizationInput
        {
            ProposalReleaseThreshold = proposalReleaseThreshold.Clone()
        };

        var minerParliamentContractStub = GetParliamentContractTester(InitialMinersKeyPairs[0]);

        {
            var transactionResult =
                await ParliamentContractStub.CreateOrganization.SendWithExceptionAsync(createOrganizationInput);
            transactionResult.TransactionResult.Error.ShouldContain("Unauthorized to create organization.");
        }

        {
            createOrganizationInput.ProposalReleaseThreshold = proposalReleaseThreshold;
            createOrganizationInput.ProposalReleaseThreshold.MinimalApprovalThreshold = 10000;
            createOrganizationInput.ProposalReleaseThreshold.MinimalVoteThreshold = 10000;
            createOrganizationInput.ProposalReleaseThreshold.MaximalAbstentionThreshold = 0;
            createOrganizationInput.ProposalReleaseThreshold.MaximalRejectionThreshold = 0;
            var transactionResult =
                await minerParliamentContractStub.CreateOrganization.SendAsync(createOrganizationInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        {
            createOrganizationInput.ProposalReleaseThreshold = proposalReleaseThreshold;
            createOrganizationInput.ProposalReleaseThreshold.MinimalApprovalThreshold = 0;
            var transactionResult =
                await minerParliamentContractStub.CreateOrganization.SendWithExceptionAsync(createOrganizationInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
        }

        {
            createOrganizationInput.ProposalReleaseThreshold = proposalReleaseThreshold;
            createOrganizationInput.ProposalReleaseThreshold.MinimalApprovalThreshold = -1;
            var transactionResult =
                await minerParliamentContractStub.CreateOrganization.SendWithExceptionAsync(createOrganizationInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
        }

        {
            createOrganizationInput.ProposalReleaseThreshold = proposalReleaseThreshold;
            createOrganizationInput.ProposalReleaseThreshold.MaximalAbstentionThreshold = -1;
            var transactionResult =
                await minerParliamentContractStub.CreateOrganization.SendWithExceptionAsync(createOrganizationInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
        }

        {
            createOrganizationInput.ProposalReleaseThreshold = proposalReleaseThreshold;
            createOrganizationInput.ProposalReleaseThreshold.MaximalAbstentionThreshold = 3334;
            var transactionResult =
                await minerParliamentContractStub.CreateOrganization.SendWithExceptionAsync(createOrganizationInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
        }

        {
            createOrganizationInput.ProposalReleaseThreshold = proposalReleaseThreshold;
            createOrganizationInput.ProposalReleaseThreshold.MaximalRejectionThreshold = 3334;
            var transactionResult =
                await minerParliamentContractStub.CreateOrganization.SendWithExceptionAsync(createOrganizationInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
        }

        {
            createOrganizationInput.ProposalReleaseThreshold = proposalReleaseThreshold;
            createOrganizationInput.ProposalReleaseThreshold.MinimalApprovalThreshold = 10001;
            var transactionResult =
                await minerParliamentContractStub.CreateOrganization.SendWithExceptionAsync(createOrganizationInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Create_ProposalFailed_Test()
    {
        await InitializeParliamentContracts();
        var minimalApprovalThreshold = 6667;
        var maximalAbstentionThreshold = 2000;
        var maximalRejectionThreshold = 3000;
        var minimalVoteThreshold = 8000;
        var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);
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
                await ParliamentContractStub.CreateProposal.SendWithExceptionAsync(createProposalInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
        }
        //ToAddress is null
        {
            createProposalInput.ContractMethodName = "Test";
            createProposalInput.ToAddress = null;

            var transactionResult =
                await ParliamentContractStub.CreateProposal.SendWithExceptionAsync(createProposalInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
        }
        //ExpiredTime is null
        {
            createProposalInput.ExpiredTime = null;
            createProposalInput.ToAddress = Accounts[0].Address;

            var transactionResult =
                await ParliamentContractStub.CreateProposal.SendWithExceptionAsync(createProposalInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
        }
        //"Expired proposal."
        {
            createProposalInput.ExpiredTime = TimestampHelper.GetUtcNow().AddSeconds(-1);
            var transactionResult =
                await ParliamentContractStub.CreateProposal.SendWithExceptionAsync(createProposalInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        }
        //"No registered organization."
        {
            createProposalInput.ExpiredTime = BlockTimeProvider.GetBlockTime().AddDays(1);
            createProposalInput.OrganizationAddress = Accounts[1].Address;

            var transactionResult =
                await ParliamentContractStub.CreateProposal.SendWithExceptionAsync(createProposalInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("No registered organization.").ShouldBeTrue();
        }
        //"Proposal with same input."
        {
            createProposalInput.OrganizationAddress = organizationAddress;
            var transactionResult1 = await ParliamentContractStub.CreateProposal.SendAsync(createProposalInput);
            transactionResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var transactionResult2 = await ParliamentContractStub.CreateProposal.SendAsync(createProposalInput);
            transactionResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
        // invalid proposal description url
        {
            createProposalInput.ProposalDescriptionUrl = "www.abc.com";
            var transactionResult =
                await ParliamentContractStub.CreateProposal.SendWithExceptionAsync(createProposalInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();

            createProposalInput.ProposalDescriptionUrl = "http://www.abc.com";
            var transactionResult1 = await ParliamentContractStub.CreateProposal.SendAsync(createProposalInput);
            transactionResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
    }

    [Fact]
    public async Task Approve_Proposal_NotFoundProposal_Test()
    {
        var transactionResult =
            await ParliamentContractStub.Approve.SendWithExceptionAsync(HashHelper.ComputeFrom("Test"));
        transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
    }

    [Fact]
    public async Task Approve_Proposal_NotAuthorizedApproval_Test()
    {
        await InitializeParliamentContracts();
        var minimalApprovalThreshold = 6667;
        var maximalAbstentionThreshold = 2000;
        var maximalRejectionThreshold = 3000;
        var minimalVoteThreshold = 8000;
        var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);
        var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);

        ParliamentContractStub = GetParliamentContractTester(TesterKeyPair);
        var transactionResult = await ParliamentContractStub.Approve.SendWithExceptionAsync(proposalId);
        transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        transactionResult.TransactionResult.Error.Contains("Unauthorized sender.").ShouldBeTrue();
    }

    [Fact]
    public async Task Approve_Proposal_ExpiredTime_Test()
    {
        await InitializeParliamentContracts();
        var minimalApprovalThreshold = 6667;
        var maximalAbstentionThreshold = 2000;
        var maximalRejectionThreshold = 3000;
        var minimalVoteThreshold = 8000;
        var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);
        var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);

        var parliamentContractStub = GetParliamentContractTester(InitialMinersKeyPairs[0]);
        BlockTimeProvider.SetBlockTime(BlockTimeProvider.GetBlockTime().AddDays(5));
        var error = await parliamentContractStub.Approve.CallWithExceptionAsync(proposalId);
        error.Value.ShouldContain("Invalid proposal.");
    }

    [Fact]
    public async Task Approve_Proposal_ApprovalAlreadyExists_Test()
    {
        await InitializeParliamentContracts();
        var minimalApprovalThreshold = 6667;
        var maximalAbstentionThreshold = 2000;
        var maximalRejectionThreshold = 3000;
        var minimalVoteThreshold = 8000;
        var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);
        var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);

        ParliamentContractStub = GetParliamentContractTester(InitialMinersKeyPairs[0]);
        var transactionResult1 =
            await ParliamentContractStub.Approve.SendAsync(proposalId);
        transactionResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var transactionResult2 =
            await ParliamentContractStub.Reject.SendWithExceptionAsync(proposalId);
        transactionResult2.TransactionResult.Error.Contains("Already approved").ShouldBeTrue();

        var transactionResult3 =
            await ParliamentContractStub.Abstain.SendWithExceptionAsync(proposalId);
        transactionResult3.TransactionResult.Error.Contains("Already approved").ShouldBeTrue();
    }

    [Fact]
    public async Task Reject_Without_Authority_Test()
    {
        await InitializeParliamentContracts();
        var minimalApprovalThreshold = 6667;
        var maximalAbstentionThreshold = 2000;
        var maximalRejectionThreshold = 3000;
        var minimalVoteThreshold = 8000;
        var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);
        var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
        ParliamentContractStub = GetParliamentContractTester(TesterKeyPair);
        var transactionResult1 =
            await ParliamentContractStub.Reject.SendWithExceptionAsync(proposalId);
        transactionResult1.TransactionResult.Error.ShouldContain("Unauthorized sender");
    }

    [Fact]
    public async Task Reject_With_Invalid_Proposal_Test()
    {
        await InitializeParliamentContracts();
        var minimalApprovalThreshold = 6667;
        var maximalAbstentionThreshold = 2000;
        var maximalRejectionThreshold = 3000;
        var minimalVoteThreshold = 8000;
        var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);

        ParliamentContractStub = GetParliamentContractTester(InitialMinersKeyPairs[0]);

        // proposal does not exist
        {
            var transactionResult1 = await ParliamentContractStub.Reject.SendWithExceptionAsync(new Hash());
            transactionResult1.TransactionResult.Error.ShouldContain("Proposal not found");
        }

        //proposal expired
        {
            var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
            BlockTimeProvider.SetBlockTime(BlockTimeProvider.GetBlockTime().AddDays(5));
            var transactionResult1 =
                await ParliamentContractStub.Reject.SendWithExceptionAsync(proposalId);
            transactionResult1.TransactionResult.Error.ShouldContain("Invalid proposal");
        }
    }

    [Fact]
    public async Task Reject_Approved_Proposal_Test()
    {
        await InitializeParliamentContracts();
        var minimalApprovalThreshold = 6667;
        var maximalAbstentionThreshold = 2000;
        var maximalRejectionThreshold = 3000;
        var minimalVoteThreshold = 8000;
        var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);

        ParliamentContractStub = GetParliamentContractTester(InitialMinersKeyPairs[0]);
        var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
        await ParliamentContractStub.Approve.SendAsync(proposalId);
        var transactionResult1 =
            await ParliamentContractStub.Reject.SendWithExceptionAsync(proposalId);
        transactionResult1.TransactionResult.Error.ShouldContain("Already approved");
    }

    [Fact]
    public async Task Reject_Success_Test()
    {
        await InitializeParliamentContracts();
        var minimalApprovalThreshold = 6667;
        var maximalAbstentionThreshold = 2000;
        var maximalRejectionThreshold = 3000;
        var minimalVoteThreshold = 8000;
        var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);

        ParliamentContractStub = GetParliamentContractTester(InitialMinersKeyPairs[0]);
        var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
        await ParliamentContractStub.Reject.SendAsync(proposalId);
        var proposal = await ParliamentContractStub.GetProposal.CallAsync(proposalId);
        proposal.RejectionCount.ShouldBe(1);
    }

    [Fact]
    public async Task Abstain_Without_Authority_Test()
    {
        await InitializeParliamentContracts();
        var minimalApprovalThreshold = 6667;
        var maximalAbstentionThreshold = 2000;
        var maximalRejectionThreshold = 3000;
        var minimalVoteThreshold = 8000;
        var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);
        var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
        ParliamentContractStub = GetParliamentContractTester(TesterKeyPair);
        var transactionResult1 =
            await ParliamentContractStub.Abstain.SendWithExceptionAsync(proposalId);
        transactionResult1.TransactionResult.Error.ShouldContain("Unauthorized sender");
    }

    [Fact]
    public async Task Abstain_With_Invalid_Proposal_Test()
    {
        await InitializeParliamentContracts();
        var minimalApprovalThreshold = 6667;
        var maximalAbstentionThreshold = 2000;
        var maximalRejectionThreshold = 3000;
        var minimalVoteThreshold = 8000;
        var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);

        ParliamentContractStub = GetParliamentContractTester(InitialMinersKeyPairs[0]);

        // proposal does not exist
        {
            var transactionResult1 = await ParliamentContractStub.Abstain.SendWithExceptionAsync(new Hash());
            transactionResult1.TransactionResult.Error.ShouldContain("Proposal not found");
        }

        //proposal expired
        {
            var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
            BlockTimeProvider.SetBlockTime(BlockTimeProvider.GetBlockTime().AddDays(5));
            var transactionResult1 =
                await ParliamentContractStub.Abstain.SendWithExceptionAsync(proposalId);
            transactionResult1.TransactionResult.Error.ShouldContain("Invalid proposal");
        }
    }

    [Fact]
    public async Task Abstain_Approved_Proposal_Test()
    {
        await InitializeParliamentContracts();
        var minimalApprovalThreshold = 6667;
        var maximalAbstentionThreshold = 2000;
        var maximalRejectionThreshold = 3000;
        var minimalVoteThreshold = 8000;
        var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);

        ParliamentContractStub = GetParliamentContractTester(InitialMinersKeyPairs[0]);
        var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
        await ParliamentContractStub.Approve.SendAsync(proposalId);
        var transactionResult1 =
            await ParliamentContractStub.Abstain.SendWithExceptionAsync(proposalId);
        transactionResult1.TransactionResult.Error.ShouldContain("Already approved");
    }

    [Fact]
    public async Task Abstain_Success_Test()
    {
        await InitializeParliamentContracts();
        var minimalApprovalThreshold = 6667;
        var maximalAbstentionThreshold = 2000;
        var maximalRejectionThreshold = 3000;
        var minimalVoteThreshold = 8000;
        var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);

        ParliamentContractStub = GetParliamentContractTester(InitialMinersKeyPairs[0]);
        var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
        await ParliamentContractStub.Abstain.SendAsync(proposalId);
        var proposal = await ParliamentContractStub.GetProposal.CallAsync(proposalId);
        proposal.AbstentionCount.ShouldBe(1);
    }

    [Fact]
    public async Task Check_Proposal_ToBeReleased()
    {
        await InitializeParliamentContracts();

        {
            var minimalApprovalThreshold = 3000;
            var maximalAbstentionThreshold = 3000;
            var maximalRejectionThreshold = 3000;
            var minimalVoteThreshold = 6000;
            var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
                maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);

            // Rejection probability > maximalRejectionThreshold
            {
                var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
                await TransferToOrganizationAddressAsync(organizationAddress);
                //Voted reviewer is not enough
                await ApproveAsync(InitialMinersKeyPairs[0], proposalId);
                ParliamentContractStub = GetParliamentContractTester(DefaultSenderKeyPair);
                var proposal = await ParliamentContractStub.GetProposal.CallAsync(proposalId);
                proposal.ToBeReleased.ShouldBeFalse();
                //Approve probability > minimalApprovalThreshold
                await ApproveAsync(InitialMinersKeyPairs[1], proposalId);
                ParliamentContractStub = GetParliamentContractTester(DefaultSenderKeyPair);
                proposal = await ParliamentContractStub.GetProposal.CallAsync(proposalId);
                proposal.ToBeReleased.ShouldBeTrue();
                //Rejection probability > maximalRejectionThreshold
                await RejectionAsync(InitialMinersKeyPairs[2], proposalId);
                ParliamentContractStub = GetParliamentContractTester(DefaultSenderKeyPair);
                proposal = await ParliamentContractStub.GetProposal.CallAsync(proposalId);
                proposal.ToBeReleased.ShouldBeFalse();
            }
            // Abstain probability > maximalAbstentionThreshold
            {
                var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
                await TransferToOrganizationAddressAsync(organizationAddress);
                //Voted reviewer is not enough
                await ApproveAsync(InitialMinersKeyPairs[0], proposalId);
                ParliamentContractStub = GetParliamentContractTester(DefaultSenderKeyPair);
                var proposal = await ParliamentContractStub.GetProposal.CallAsync(proposalId);
                proposal.ToBeReleased.ShouldBeFalse();
                //Approve probability > minimalApprovalThreshold
                await ApproveAsync(InitialMinersKeyPairs[1], proposalId);
                ParliamentContractStub = GetParliamentContractTester(DefaultSenderKeyPair);
                proposal = await ParliamentContractStub.GetProposal.CallAsync(proposalId);
                proposal.ToBeReleased.ShouldBeTrue();
                //Abstain probability > maximalAbstentionThreshold
                await AbstainAsync(InitialMinersKeyPairs[2], proposalId);
                ParliamentContractStub = GetParliamentContractTester(DefaultSenderKeyPair);
                proposal = await ParliamentContractStub.GetProposal.CallAsync(proposalId);
                proposal.ToBeReleased.ShouldBeFalse();
            }
        }
        {
            var minimalApprovalThreshold = 3000;
            var maximalAbstentionThreshold = 6000;
            var maximalRejectionThreshold = 3000;
            var minimalVoteThreshold = 6000;
            var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
                maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);
            {
                var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
                await TransferToOrganizationAddressAsync(organizationAddress);
                //Approve probability > minimalApprovalThreshold
                await AbstainAsync(InitialMinersKeyPairs[0], proposalId);
                await ApproveAsync(InitialMinersKeyPairs[1], proposalId);
                ParliamentContractStub = GetParliamentContractTester(DefaultSenderKeyPair);
                var proposal = await ParliamentContractStub.GetProposal.CallAsync(proposalId);
                proposal.ToBeReleased.ShouldBeTrue();
            }
        }
    }

    [Fact]
    public async Task Release_NotEnoughApprove_Test()
    {
        await InitializeParliamentContracts();
        var minimalApprovalThreshold = 6667;
        var maximalAbstentionThreshold = 2000;
        var maximalRejectionThreshold = 3000;
        var minimalVoteThreshold = 8000;
        var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);
        var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
        await TransferToOrganizationAddressAsync(organizationAddress);
        //Reviewer Shares < ReleaseThreshold, release failed
        await ApproveAsync(InitialMinersKeyPairs[0], proposalId);
        ParliamentContractStub = GetParliamentContractTester(DefaultSenderKeyPair);
        var result = await ParliamentContractStub.Release.SendWithExceptionAsync(proposalId);
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        result.TransactionResult.Error.Contains("Not approved.").ShouldBeTrue();
    }

    [Fact]
    public async Task Release_NotFound_Test()
    {
        var proposalId = HashHelper.ComputeFrom("test");
        var result = await ParliamentContractStub.Release.SendWithExceptionAsync(proposalId);
        //Proposal not found
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        result.TransactionResult.Error.Contains("Proposal not found.").ShouldBeTrue();
    }

    [Fact]
    public async Task Release_WrongSender_Test()
    {
        await InitializeParliamentContracts();
        var minimalApprovalThreshold = 6667;
        var maximalAbstentionThreshold = 2000;
        var maximalRejectionThreshold = 3000;
        var minimalVoteThreshold = 8000;
        var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);
        var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
        await TransferToOrganizationAddressAsync(organizationAddress);
        await ApproveAsync(InitialMinersKeyPairs[0], proposalId);
        await ApproveAsync(InitialMinersKeyPairs[1], proposalId);

        ParliamentContractStub = GetParliamentContractTester(TesterKeyPair);
        var result = await ParliamentContractStub.Release.SendWithExceptionAsync(proposalId);
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        result.TransactionResult.Error.Contains("No permission.").ShouldBeTrue();
    }

    [Fact]
    public async Task Release_Expired_Proposal_Test()
    {
        await InitializeParliamentContracts();
        var minimalApprovalThreshold = 6667;
        var maximalAbstentionThreshold = 2000;
        var maximalRejectionThreshold = 3000;
        var minimalVoteThreshold = 8000;
        var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);
        var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
        ParliamentContractStub = GetParliamentContractTester(DefaultSenderKeyPair);
        BlockTimeProvider.SetBlockTime(BlockTimeProvider.GetBlockTime().AddDays(5));
        var result = await ParliamentContractStub.Release.SendWithExceptionAsync(proposalId);
        result.TransactionResult.Error.ShouldContain("Invalid proposal");
    }

    [Fact]
    public async Task Release_Proposal_Test()
    {
        await InitializeParliamentContracts();
        var minimalApprovalThreshold = 6667;
        var maximalAbstentionThreshold = 2000;
        var maximalRejectionThreshold = 3000;
        var minimalVoteThreshold = 8000;
        var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);
        var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
        await TransferToOrganizationAddressAsync(organizationAddress);
        await ApproveAsync(InitialMinersKeyPairs[0], proposalId);
        await ApproveAsync(InitialMinersKeyPairs[1], proposalId);
        await ApproveAsync(InitialMinersKeyPairs[2], proposalId);

        ParliamentContractStub = GetParliamentContractTester(DefaultSenderKeyPair);
        var result = await ParliamentContractStub.Release.SendAsync(proposalId);
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        // Check inline transaction result
        var getBalance = TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
        {
            Symbol = "ELF",
            Owner = Tester
        }).Result.Balance;
        getBalance.ShouldBe(100);

        var proposalInfo = await ParliamentContractStub.GetProposal.CallAsync(proposalId);
        proposalInfo.ShouldBe(new ProposalOutput());
    }

    [Fact]
    public async Task Release_Proposal_AlreadyReleased_Test()
    {
        await InitializeParliamentContracts();
        var minimalApprovalThreshold = 6667;
        var maximalAbstentionThreshold = 2000;
        var maximalRejectionThreshold = 3000;
        var minimalVoteThreshold = 8000;
        var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);
        var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
        await TransferToOrganizationAddressAsync(organizationAddress);
        await ApproveAsync(InitialMinersKeyPairs[0], proposalId);
        await ApproveAsync(InitialMinersKeyPairs[1], proposalId);
        await ApproveAsync(InitialMinersKeyPairs[2], proposalId);

        ParliamentContractStub = GetParliamentContractTester(DefaultSenderKeyPair);
        var txResult1 = await ParliamentContractStub.Release.SendAsync(proposalId);
        txResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        ParliamentContractStub = GetParliamentContractTester(InitialMinersKeyPairs[2]);
        var transactionResult2 =
            await ParliamentContractStub.Approve.SendWithExceptionAsync(proposalId);
        transactionResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        transactionResult2.TransactionResult.Error.Contains("Proposal not found.").ShouldBeTrue();

        ParliamentContractStub = GetParliamentContractTester(DefaultSenderKeyPair);
        var transactionResult3 =
            await ParliamentContractStub.Release.SendWithExceptionAsync(proposalId);
        transactionResult3.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        transactionResult3.TransactionResult.Error.Contains("Proposal not found.").ShouldBeTrue();
    }

    [Fact]
    public async Task Change_OrganizationThreshold_With_Invalid_Sender_Test()
    {
        var changeOrganizationThresholdRet =
            await ParliamentContractStub.ChangeOrganizationThreshold.SendWithExceptionAsync(
                new ProposalReleaseThreshold());
        changeOrganizationThresholdRet.TransactionResult.Error.ShouldContain("Organization not found");
    }

    [Fact]
    public async Task Change_OrganizationThreshold_Test()
    {
        await InitializeParliamentContracts();
        var minimalApprovalThreshold = 3000;
        var maximalAbstentionThreshold = 3000;
        var maximalRejectionThreshold = 3000;
        var minimalVoteThreshold = 3000;
        var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);
        var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
        await ApproveAsync(InitialMinersKeyPairs[0], proposalId);
        var proposal = await ParliamentContractStub.GetProposal.CallAsync(proposalId);
        proposal.ToBeReleased.ShouldBeTrue();

        {
            var proposalReleaseThresholdInput = new ProposalReleaseThreshold
            {
                MinimalVoteThreshold = 6000
            };
            var createProposalInput = new CreateProposalInput
            {
                ContractMethodName = nameof(ParliamentContractStub.ChangeOrganizationThreshold),
                ToAddress = ParliamentContractAddress,
                Params = proposalReleaseThresholdInput.ToByteString(),
                ExpiredTime = BlockTimeProvider.GetBlockTime().AddDays(2),
                OrganizationAddress = organizationAddress
            };
            var changeProposal = await ParliamentContractStub.CreateProposal.SendAsync(createProposalInput);
            changeProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var changeProposalId = changeProposal.Output;
            await ApproveAsync(InitialMinersKeyPairs[0], changeProposalId);
            var result = await ParliamentContractStub.Release.SendWithExceptionAsync(changeProposalId);
            result.TransactionResult.Error.ShouldContain("Invalid organization.");
        }
        {
            var proposalReleaseThresholdInput = new ProposalReleaseThreshold
            {
                MinimalVoteThreshold = 6000,
                MinimalApprovalThreshold = minimalApprovalThreshold
            };
            var createProposalInput = new CreateProposalInput
            {
                ContractMethodName = nameof(ParliamentContractStub.ChangeOrganizationThreshold),
                ToAddress = ParliamentContractAddress,
                Params = proposalReleaseThresholdInput.ToByteString(),
                ExpiredTime = BlockTimeProvider.GetBlockTime().AddDays(2),
                OrganizationAddress = organizationAddress
            };
            var changeProposal = await ParliamentContractStub.CreateProposal.SendAsync(createProposalInput);
            changeProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var changeProposalId = changeProposal.Output;
            await ApproveAsync(InitialMinersKeyPairs[0], changeProposalId);
            var result = await ParliamentContractStub.Release.SendAsync(changeProposalId);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var organizationInfo = await ParliamentContractStub.GetOrganization.CallAsync(organizationAddress);
            organizationInfo.ProposalReleaseThreshold.MinimalVoteThreshold.ShouldBe(proposalReleaseThresholdInput
                .MinimalVoteThreshold);
            organizationInfo.ProposalReleaseThreshold.MinimalApprovalThreshold.ShouldBe(minimalApprovalThreshold);
        }
    }

    [Fact]
    public async Task Check_ValidProposal_Test()
    {
        await InitializeParliamentContracts();

        var minimalApprovalThreshold = 6000;
        var maximalAbstentionThreshold = 2000;
        var maximalRejectionThreshold = 3000;
        var minimalVoteThreshold = 6000;
        var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);
        var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
        await TransferToOrganizationAddressAsync(organizationAddress);

        //Get valid Proposal
        var parliamentContractStub = GetParliamentContractTester(InitialMinersKeyPairs[2]);
        var validProposals = await parliamentContractStub.GetNotVotedPendingProposals.CallAsync(new ProposalIdList
        {
            ProposalIds = { proposalId }
        });
        validProposals.ProposalIds.Count.ShouldBe(1);
        var notVotedProposals = await parliamentContractStub.GetNotVotedProposals.CallAsync(new ProposalIdList
        {
            ProposalIds = { proposalId }
        });
        notVotedProposals.ProposalIds.Count.ShouldBe(1);

        await ApproveAsync(InitialMinersKeyPairs[0], proposalId);
        await ApproveAsync(InitialMinersKeyPairs[1], proposalId);
        validProposals = await parliamentContractStub.GetNotVotedPendingProposals.CallAsync(new ProposalIdList
        {
            ProposalIds = { proposalId }
        });
        validProposals.ProposalIds.Count.ShouldBe(0);

        parliamentContractStub = GetParliamentContractTester(InitialMinersKeyPairs[2]);
        validProposals = await parliamentContractStub.GetNotVotedPendingProposals.CallAsync(new ProposalIdList
        {
            ProposalIds = { proposalId }
        });
        validProposals.ProposalIds.Count.ShouldBe(0);
        notVotedProposals = await parliamentContractStub.GetNotVotedProposals.CallAsync(new ProposalIdList
        {
            ProposalIds = { proposalId }
        });
        notVotedProposals.ProposalIds.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Clear_NotExpiredProposal_Test()
    {
        await InitializeParliamentContracts();
        var defaultParliamentAddress =
            await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        var miner = InitialMinersKeyPairs[1];
        // proposal does not exist
        {
            var clearProposalRet = await ParliamentContractStub.ClearProposal.SendWithExceptionAsync(new Hash());
            clearProposalRet.TransactionResult.Error.ShouldContain("Proposal clear failed");
        }

        // proposal is not expired
        {
            var proposalId = await CreateProposalAsync(miner, defaultParliamentAddress);
            var clearProposalRet = await ParliamentContractStub.ClearProposal.SendWithExceptionAsync(proposalId);
            clearProposalRet.TransactionResult.Error.ShouldContain("Proposal clear failed");
        }
    }

    [Fact]
    public async Task Clear_ExpiredProposal_Test()
    {
        await InitializeParliamentContracts();

        var minimalApprovalThreshold = 6667;
        var maximalAbstentionThreshold = 2000;
        var maximalRejectionThreshold = 3000;
        var minimalVoteThreshold = 8000;
        var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);
        var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);

        ParliamentContractStub = GetParliamentContractTester(InitialMinersKeyPairs[0]);
        BlockTimeProvider.SetBlockTime(BlockTimeProvider.GetBlockTime().AddDays(5));
        var error = await ParliamentContractStub.Approve.CallWithExceptionAsync(proposalId);
        error.Value.ShouldContain("Invalid proposal.");

        var clear = await ParliamentContractStub.ClearProposal.SendAsync(proposalId);
        clear.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var proposal = await ParliamentContractStub.GetProposal.CallAsync(proposalId);
        proposal.ShouldBe(new ProposalOutput());
    }

    [Fact]
    public async Task ChangeMethodFeeController_Test()
    {
        await InitializeParliamentContracts();
        var parliamentContractStub = GetParliamentContractTester(InitialMinersKeyPairs[0]);
        var createOrganizationResult =
            await parliamentContractStub.CreateOrganization.SendAsync(
                new CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MinimalApprovalThreshold = 1000,
                        MinimalVoteThreshold = 1000
                    }
                });
        var organizationAddress = Address.Parser.ParseFrom(createOrganizationResult.TransactionResult.ReturnValue);

        var methodFeeController = await parliamentContractStub.GetMethodFeeController.CallAsync(new Empty());
        var defaultOrganization = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        methodFeeController.OwnerAddress.ShouldBe(defaultOrganization);

        const string proposalCreationMethodName = nameof(parliamentContractStub.ChangeMethodFeeController);
        var proposalId = await CreateFeeProposalAsync(ParliamentContractAddress,
            methodFeeController.OwnerAddress, proposalCreationMethodName, new AuthorityInfo
            {
                OwnerAddress = organizationAddress,
                ContractAddress = ParliamentContractAddress
            });
        await ApproveAsync(InitialMinersKeyPairs[0], proposalId);
        await ApproveAsync(InitialMinersKeyPairs[1], proposalId);
        await ApproveAsync(InitialMinersKeyPairs[2], proposalId);

        var releaseResult = await parliamentContractStub.Release.SendAsync(proposalId);
        releaseResult.TransactionResult.Error.ShouldBeNullOrEmpty();
        releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var newMethodFeeController = await parliamentContractStub.GetMethodFeeController.CallAsync(new Empty());
        newMethodFeeController.OwnerAddress.ShouldBe(organizationAddress);
    }

    [Fact]
    public async Task ChangeMethodFeeController_WithoutAuth_Test()
    {
        await InitializeParliamentContracts();
        var minimalApprovalThreshold = 6667;
        var maximalAbstentionThreshold = 2000;
        var maximalRejectionThreshold = 3000;
        var minimalVoteThreshold = 8000;
        var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);

        var parliamentContractStub = GetParliamentContractTester(InitialMinersKeyPairs[0]);
        var result = await parliamentContractStub.ChangeMethodFeeController.SendWithExceptionAsync(
            new AuthorityInfo
            {
                OwnerAddress = organizationAddress,
                ContractAddress = ParliamentContractAddress
            });

        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        result.TransactionResult.Error.Contains("Unauthorized behavior.").ShouldBeTrue();
    }

    [Fact]
    public async Task ChangeMethodFeeController_With_Invalid_Authority_Test()
    {
        await InitializeParliamentContracts();
        var parliamentContractStub = GetParliamentContractTester(InitialMinersKeyPairs[0]);


        var methodFeeController = await parliamentContractStub.GetMethodFeeController.CallAsync(new Empty());
        var defaultOrganization = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        methodFeeController.OwnerAddress.ShouldBe(defaultOrganization);

        const string proposalCreationMethodName = nameof(parliamentContractStub.ChangeMethodFeeController);
        var proposalId = await CreateFeeProposalAsync(ParliamentContractAddress,
            methodFeeController.OwnerAddress, proposalCreationMethodName, new AuthorityInfo
            {
                OwnerAddress = ParliamentContractAddress,
                ContractAddress = ParliamentContractAddress
            });
        await ApproveAsync(InitialMinersKeyPairs[0], proposalId);
        await ApproveAsync(InitialMinersKeyPairs[1], proposalId);
        await ApproveAsync(InitialMinersKeyPairs[2], proposalId);

        var releaseResult = await parliamentContractStub.Release.SendWithExceptionAsync(proposalId);
        releaseResult.TransactionResult.Error.ShouldContain("Invalid authority input");
    }

    [Fact]
    public async Task SetMethodFee_With_Invalid_Input_Test()
    {
        // token symbol does not exist
        {
            var tokenSymbol = "NOTEXIST";
            var setMethodFeeRet = await ParliamentContractStub.SetMethodFee.SendWithExceptionAsync(new MethodFees
            {
                MethodName = nameof(ParliamentContractStub.Abstain),
                Fees =
                {
                    new MethodFee
                    {
                        Symbol = tokenSymbol,
                        BasicFee = 100
                    }
                }
            });
            setMethodFeeRet.TransactionResult.Error.ShouldContain("token is not found");
        }

        // amount < 0
        {
            var invalidAmount = -1;
            var setMethodFeeRet = await ParliamentContractStub.SetMethodFee.SendWithExceptionAsync(new MethodFees
            {
                MethodName = nameof(ParliamentContractStub.Abstain),
                Fees =
                {
                    new MethodFee
                    {
                        Symbol = "ELF",
                        BasicFee = invalidAmount
                    }
                }
            });
            setMethodFeeRet.TransactionResult.Error.ShouldContain("Invalid amount");
        }
    }

    [Fact]
    public async Task SetMethodFee_Without_Authority_Test()
    {
        await InitializeParliamentContracts();
        var setMethodFeeRet = await ParliamentContractStub.SetMethodFee.SendWithExceptionAsync(new MethodFees
        {
            MethodName = nameof(ParliamentContractStub.Abstain),
            Fees =
            {
                new MethodFee
                {
                    Symbol = "ELF",
                    BasicFee = 100
                }
            }
        });
        setMethodFeeRet.TransactionResult.Error.ShouldContain("Unauthorized to set method fee");
    }

    [Fact]
    public async Task SetMethodFee_Success_Test()
    {
        await InitializeParliamentContracts();
        var parliamentContractStub = GetParliamentContractTester(InitialMinersKeyPairs[0]);
        var methodFeeController = await parliamentContractStub.GetMethodFeeController.CallAsync(new Empty());
        var methodFeeName = nameof(parliamentContractStub.Abstain);
        var tokenSymbol = "ELF";
        var fee = 100;
        var methodFees = new MethodFees
        {
            MethodName = methodFeeName,
            Fees =
            {
                new MethodFee
                {
                    Symbol = tokenSymbol,
                    BasicFee = fee
                }
            }
        };
        const string proposalCreationMethodName = nameof(parliamentContractStub.SetMethodFee);
        var proposalId = await CreateFeeProposalAsync(ParliamentContractAddress,
            methodFeeController.OwnerAddress, proposalCreationMethodName, methodFees);
        await ApproveAsync(InitialMinersKeyPairs[0], proposalId);
        await ApproveAsync(InitialMinersKeyPairs[1], proposalId);
        await ApproveAsync(InitialMinersKeyPairs[2], proposalId);
        await parliamentContractStub.Release.SendAsync(proposalId);
        var methodFee = await parliamentContractStub.GetMethodFee.CallAsync(new StringValue
        {
            Value = methodFeeName
        });
        methodFee.MethodName.ShouldBe(methodFeeName);
        methodFee.Fees.Count.ShouldBe(1);
        methodFee.Fees[0].Symbol.ShouldBe(tokenSymbol);
        methodFee.Fees[0].BasicFee.ShouldBe(fee);

        // method name = ApproveMultiProposals
        var specialMethodName = nameof(parliamentContractStub.ApproveMultiProposals);
        methodFees.MethodName = specialMethodName;
        proposalId = await CreateFeeProposalAsync(ParliamentContractAddress,
            methodFeeController.OwnerAddress, proposalCreationMethodName, methodFees);
        await ApproveAsync(InitialMinersKeyPairs[0], proposalId);
        await ApproveAsync(InitialMinersKeyPairs[1], proposalId);
        await ApproveAsync(InitialMinersKeyPairs[2], proposalId);
        await parliamentContractStub.Release.SendAsync(proposalId);
        methodFee = await parliamentContractStub.GetMethodFee.CallAsync(new StringValue
        {
            Value = specialMethodName
        });
        methodFee.Fees.Count.ShouldBe(0);
    }

    [Fact]
    public async Task CreateOrganizationBySystemContract_Fail_Test()
    {
        var createOrganizationRet =
            await ParliamentContractStub.CreateOrganizationBySystemContract.SendWithExceptionAsync(
                new CreateOrganizationBySystemContractInput());
        createOrganizationRet.TransactionResult.Error.ShouldContain("Unauthorized");
    }

    [Fact]
    public async Task CreateOrganizationBySystemContract_Success_Test()
    {
        var chain = _blockchainService.GetChainAsync();
        var blockIndex = new BlockIndex
        {
            BlockHash = chain.Result.BestChainHash,
            BlockHeight = chain.Result.BestChainHeight
        };
        await _smartContractAddressService.SetSmartContractAddressAsync(blockIndex,
            _smartContractAddressNameProvider.ContractStringName, DefaultSender);

        var createOrganizationInput = new CreateOrganizationBySystemContractInput
        {
            OrganizationCreationInput = new CreateOrganizationInput
            {
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MinimalApprovalThreshold = 1000,
                    MinimalVoteThreshold = 1000
                }
            },
            OrganizationAddressFeedbackMethod = string.Empty
        };
        var createOrganizationRet =
            await ParliamentContractStub.CreateOrganizationBySystemContract.SendAsync(createOrganizationInput);
        createOrganizationRet.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }

    [Fact]
    public async Task ValidateOrganizationExist_Test()
    {
        await InitializeParliamentContracts();
        var minimalApprovalThreshold = 6667;
        var maximalAbstentionThreshold = 2000;
        var maximalRejectionThreshold = 3000;
        var minimalVoteThreshold = 8000;
        var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);
        var isOrganizationExist =
            await ParliamentContractStub.ValidateOrganizationExist.CallAsync(organizationAddress);
        isOrganizationExist.Value.ShouldBeTrue();

        isOrganizationExist =
            await ParliamentContractStub.ValidateOrganizationExist.CallAsync(ParliamentContractAddress);
        isOrganizationExist.Value.ShouldBeFalse();
    }

    [Fact]
    public async Task ValidateProposerInWhiteList_Test()
    {
        var proposer = DefaultSender;
        await ParliamentContractStub.Initialize.SendAsync(new InitializeInput
        {
            PrivilegedProposer = proposer
        });
        var isProposerInWhitelist =
            await ParliamentContractStub.ValidateProposerInWhiteList.CallAsync(new ValidateProposerInWhiteListInput
            {
                Proposer = proposer
            });
        isProposerInWhitelist.Value.ShouldBeTrue();

        isProposerInWhitelist =
            await ParliamentContractStub.ValidateProposerInWhiteList.CallAsync(new ValidateProposerInWhiteListInput());
        isProposerInWhitelist.Value.ShouldBeFalse();
    }

    [Fact]
    public async Task CreateProposalBySystemContract_Fail_Test()
    {
        // not be authorized
        {
            var ret = await ParliamentContractStub.CreateProposalBySystemContract.SendWithExceptionAsync(
                new CreateProposalBySystemContractInput());
            ret.TransactionResult.Error.ShouldContain("Unauthorized to propose");
        }

        var chain = _blockchainService.GetChainAsync();
        var blockIndex = new BlockIndex
        {
            BlockHash = chain.Result.BestChainHash,
            BlockHeight = chain.Result.BestChainHeight
        };
        await _smartContractAddressService.SetSmartContractAddressAsync(blockIndex,
            _smartContractAddressNameProvider.ContractStringName, DefaultSender);

        // invalid organization
        {
            var invalidInput = new CreateProposalBySystemContractInput
            {
                ProposalInput = new CreateProposalInput
                {
                    OrganizationAddress = ParliamentContractAddress
                },
                OriginProposer = DefaultSender
            };
            var ret = await ParliamentContractStub.CreateProposalBySystemContract.SendWithExceptionAsync(
                invalidInput);
            ret.TransactionResult.Error.ShouldContain("No registered organization");
        }
    }

    [Fact]
    public async Task CreateProposalBySystemContract_Success_Test()
    {
        await ParliamentContractStub.Initialize.SendAsync(new InitializeInput
        {
            PrivilegedProposer = DefaultSender
        });
        var defaultParliamentAddress =
            await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        var chain = _blockchainService.GetChainAsync();
        var blockIndex = new BlockIndex
        {
            BlockHash = chain.Result.BestChainHash,
            BlockHeight = chain.Result.BestChainHeight
        };
        await _smartContractAddressService.SetSmartContractAddressAsync(blockIndex,
            _smartContractAddressNameProvider.ContractStringName, DefaultSender);
        var input = new CreateProposalBySystemContractInput
        {
            ProposalInput = new CreateProposalInput
            {
                OrganizationAddress = defaultParliamentAddress,
                ToAddress = TokenContractAddress,
                ContractMethodName = nameof(TokenContractImplContainer.TokenContractImplStub.Transfer),
                Params = new TransferInput
                {
                    Amount = 100,
                    Symbol = "ELF",
                    To = DefaultSender
                }.ToByteString(),
                ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1)
            },
            OriginProposer = DefaultSender
        };
        var ret = await ParliamentContractStub.CreateProposalBySystemContract.SendAsync(
            input);
        ret.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }

    [Fact]
    public async Task GetNotVotedProposals_With_Invalid_Proposal_Test()
    {
        // proposal does not exist
        {
            var invalidProposalId = HashHelper.ComputeFrom("invalid hash");
            var notVotedProposals = await ParliamentContractStub.GetNotVotedProposals.CallAsync(new ProposalIdList
            {
                ProposalIds = { invalidProposalId }
            });
            notVotedProposals.ProposalIds.Count.ShouldBe(0);
        }

        //fail to validate proposal, proposal expires
        {
            await InitializeParliamentContracts();
            var minimalApprovalThreshold = 6667;
            var maximalAbstentionThreshold = 2000;
            var maximalRejectionThreshold = 3000;
            var minimalVoteThreshold = 8000;
            var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
                maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);
            var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);

            ParliamentContractStub = GetParliamentContractTester(InitialMinersKeyPairs[0]);
            BlockTimeProvider.SetBlockTime(BlockTimeProvider.GetBlockTime().AddDays(5));
            var notVotedProposals = await ParliamentContractStub.GetNotVotedProposals.CallAsync(new ProposalIdList
            {
                ProposalIds = { proposalId }
            });
            notVotedProposals.ProposalIds.Count.ShouldBe(0);
        }
    }

    [Fact]
    public async Task GetNotVotedPendingProposals_With_Invalid_Proposal_Test()
    {
        // proposal does not exist
        {
            var invalidProposalId = HashHelper.ComputeFrom("invalid hash");
            var notVotedProposals = await ParliamentContractStub.GetNotVotedPendingProposals.CallAsync(
                new ProposalIdList
                {
                    ProposalIds = { invalidProposalId }
                });
            notVotedProposals.ProposalIds.Count.ShouldBe(0);
        }
        //fail to validate proposal, proposal expires
        {
            await InitializeParliamentContracts();
            var minimalApprovalThreshold = 6667;
            var maximalAbstentionThreshold = 2000;
            var maximalRejectionThreshold = 3000;
            var minimalVoteThreshold = 8000;
            var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
                maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);
            var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);

            ParliamentContractStub = GetParliamentContractTester(InitialMinersKeyPairs[0]);
            BlockTimeProvider.SetBlockTime(BlockTimeProvider.GetBlockTime().AddDays(5));
            var notVotedProposals = await ParliamentContractStub.GetNotVotedPendingProposals.CallAsync(
                new ProposalIdList
                {
                    ProposalIds = { proposalId }
                });
            notVotedProposals.ProposalIds.Count.ShouldBe(0);
        }
    }

    [Fact]
    public async Task ApproveMultiProposals_With_Invalid_Proposal_Test()
    {
        await InitializeParliamentContracts();
        var minimalApprovalThreshold = 6667;
        var maximalAbstentionThreshold = 2000;
        var maximalRejectionThreshold = 3000;
        var minimalVoteThreshold = 8000;
        var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);
        var invalidProposalId = HashHelper.ComputeFrom("invalid hash");
        var expiredProposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);

        ParliamentContractStub = GetParliamentContractTester(InitialMinersKeyPairs[0]);
        var transactionResult =
            await ParliamentContractStub.ApproveMultiProposals.SendAsync(new ProposalIdList
            {
                ProposalIds = { invalidProposalId, expiredProposalId }
            });
        transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }

    [Fact]
    public async Task Check_ValidProposal_With_Rejected_Test()
    {
        await InitializeParliamentContracts();

        var minimalApprovalThreshold = 6000;
        var maximalAbstentionThreshold = 1;
        var maximalRejectionThreshold = 1;
        var minimalVoteThreshold = 6000;
        var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);

        //reject proposal
        var proposalTobeRejectedId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
        var parliamentContractStub = GetParliamentContractTester(InitialMinersKeyPairs[2]);
        var validProposals = await parliamentContractStub.GetNotVotedPendingProposals.CallAsync(new ProposalIdList
        {
            ProposalIds = { proposalTobeRejectedId }
        });
        validProposals.ProposalIds.Count.ShouldBe(1);

        await RejectionAsync(InitialMinersKeyPairs[0], proposalTobeRejectedId);
        validProposals = await parliamentContractStub.GetNotVotedPendingProposals.CallAsync(new ProposalIdList
        {
            ProposalIds = { proposalTobeRejectedId }
        });
        validProposals.ProposalIds.Count.ShouldBe(0);

        //abstain proposal
        var proposalTobeAbstainedId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
        validProposals = await parliamentContractStub.GetNotVotedPendingProposals.CallAsync(new ProposalIdList
        {
            ProposalIds = { proposalTobeAbstainedId }
        });
        validProposals.ProposalIds.Count.ShouldBe(1);

        await AbstainAsync(InitialMinersKeyPairs[0], proposalTobeAbstainedId);
        validProposals = await parliamentContractStub.GetNotVotedPendingProposals.CallAsync(new ProposalIdList
        {
            ProposalIds = { proposalTobeAbstainedId }
        });
        validProposals.ProposalIds.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetReleaseThresholdReachedProposals_Test()
    {
        await InitializeParliamentContracts();

        var minimalApprovalThreshold = 3000;
        var maximalAbstentionThreshold = 3000;
        var maximalRejectionThreshold = 3000;
        var minimalVoteThreshold = 6000;
        var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);
        
        var proposalId = HashHelper.ComputeFrom("proposalId");
        var parliamentContractStub = GetParliamentContractTester(InitialMinersKeyPairs[0]);
        var reachedReleasedList = await parliamentContractStub.GetReleaseThresholdReachedProposals.CallAsync(new ProposalIdList
        {
            ProposalIds = { proposalId }
        });
        reachedReleasedList.ProposalIds.Count.ShouldBe(0);
        
        proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
        reachedReleasedList = await parliamentContractStub.GetReleaseThresholdReachedProposals.CallAsync(new ProposalIdList
        {
            ProposalIds = { proposalId }
        });
        reachedReleasedList.ProposalIds.Count.ShouldBe(0);
        
        await ApproveAsync(InitialMinersKeyPairs[0], proposalId);
        reachedReleasedList = await parliamentContractStub.GetReleaseThresholdReachedProposals.CallAsync(new ProposalIdList
        {
            ProposalIds = { proposalId }
        });
        reachedReleasedList.ProposalIds.Count.ShouldBe(0);

        await ApproveAsync(InitialMinersKeyPairs[1], proposalId);
        reachedReleasedList = await parliamentContractStub.GetReleaseThresholdReachedProposals.CallAsync(new ProposalIdList
        {
            ProposalIds = { proposalId }
        });
        reachedReleasedList.ProposalIds.Count.ShouldBe(1);
        reachedReleasedList.ProposalIds[0].ShouldBe(proposalId);
    }
    
    [Fact]
    public async Task GetAvailableProposals_Test()
    {
        await InitializeParliamentContracts();

        var minimalApprovalThreshold = 3000;
        var maximalAbstentionThreshold = 3000;
        var maximalRejectionThreshold = 3000;
        var minimalVoteThreshold = 6000;
        var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);
        
        var proposalId = HashHelper.ComputeFrom("proposalId");
        var parliamentContractStub = GetParliamentContractTester(InitialMinersKeyPairs[0]);
        var reachedReleasedList = await parliamentContractStub.GetAvailableProposals.CallAsync(new ProposalIdList
        {
            ProposalIds = { proposalId }
        });
        reachedReleasedList.ProposalIds.Count.ShouldBe(0);
        
        proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
        reachedReleasedList = await parliamentContractStub.GetAvailableProposals.CallAsync(new ProposalIdList
        {
            ProposalIds = { proposalId }
        });
        reachedReleasedList.ProposalIds.Count.ShouldBe(1);
        reachedReleasedList.ProposalIds[0].ShouldBe(proposalId);
        
        await ApproveAsync(InitialMinersKeyPairs[0], proposalId);
        reachedReleasedList = await parliamentContractStub.GetAvailableProposals.CallAsync(new ProposalIdList
        {
            ProposalIds = { proposalId }
        });
        reachedReleasedList.ProposalIds.Count.ShouldBe(1);
        reachedReleasedList.ProposalIds[0].ShouldBe(proposalId);

        await ApproveAsync(InitialMinersKeyPairs[1], proposalId);
        reachedReleasedList = await parliamentContractStub.GetAvailableProposals.CallAsync(new ProposalIdList
        {
            ProposalIds = { proposalId }
        });
        reachedReleasedList.ProposalIds.Count.ShouldBe(1);
        reachedReleasedList.ProposalIds[0].ShouldBe(proposalId);
        
        await TransferToOrganizationAddressAsync(organizationAddress);
        await ParliamentContractStub.Release.SendAsync(proposalId);
        
        reachedReleasedList = await parliamentContractStub.GetAvailableProposals.CallAsync(new ProposalIdList
        {
            ProposalIds = { proposalId }
        });
        reachedReleasedList.ProposalIds.Count.ShouldBe(0);
    }

    private async Task<Hash> CreateProposalAsync(ECKeyPair proposalKeyPair, Address organizationAddress)
    {
        var transferInput = new TransferInput
        {
            Symbol = "ELF",
            Amount = 100,
            To = Tester,
            Memo = Guid.NewGuid().ToString() //In order to generate different proposal
        };
        var createProposalInput = new CreateProposalInput
        {
            ContractMethodName = nameof(TokenContractStub.Transfer),
            ToAddress = TokenContractAddress,
            Params = transferInput.ToByteString(),
            ExpiredTime = BlockTimeProvider.GetBlockTime().AddDays(2),
            OrganizationAddress = organizationAddress
        };
        var parliamentContractStub = GetParliamentContractTester(proposalKeyPair);
        var proposal = await parliamentContractStub.CreateProposal.SendAsync(createProposalInput);
        proposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var proposalCreated = ProposalCreated.Parser.ParseFrom(proposal.TransactionResult.Logs
                .First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
            .ProposalId;
        proposal.Output.ShouldBe(proposalCreated);

        return proposal.Output;
    }

    private async Task<Address> CreateOrganizationAsync(int minimalApprovalThreshold,
        int maximalAbstentionThreshold, int maximalRejectionThreshold, int minimalVoteThreshold)
    {
        var createOrganizationInput = new CreateOrganizationInput
        {
            ProposalReleaseThreshold = new ProposalReleaseThreshold
            {
                MinimalApprovalThreshold = minimalApprovalThreshold,
                MaximalAbstentionThreshold = maximalAbstentionThreshold,
                MaximalRejectionThreshold = maximalRejectionThreshold,
                MinimalVoteThreshold = minimalVoteThreshold
            }
        };
        var parliamentContractStub = GetParliamentContractTester(InitialMinersKeyPairs[0]);
        var transactionResult =
            await parliamentContractStub.CreateOrganization.SendAsync(createOrganizationInput);
        transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        return transactionResult.Output;
    }

    private async Task TransferToOrganizationAddressAsync(Address to)
    {
        await TokenContractStub.Transfer.SendAsync(new TransferInput
        {
            Symbol = "ELF",
            Amount = 200,
            To = to,
            Memo = "transfer organization address"
        });
    }

    private async Task ApproveAsync(ECKeyPair reviewer, Hash proposalId)
    {
        var utcNow = TimestampHelper.GetUtcNow();
        BlockTimeProvider.SetBlockTime(utcNow);
        var parliamentContractStub = GetParliamentContractTester(reviewer);
        var transactionResult =
            await parliamentContractStub.Approve.SendAsync(proposalId);
        transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var receiptCreated = ReceiptCreated.Parser.ParseFrom(transactionResult.TransactionResult.Logs
            .FirstOrDefault(l => l.Name == nameof(ReceiptCreated))
            ?.NonIndexed);
        ValidateReceiptCreated(receiptCreated, Address.FromPublicKey(reviewer.PublicKey), proposalId, utcNow,
            nameof(parliamentContractStub.Approve));
    }

    private async Task RejectionAsync(ECKeyPair reviewer, Hash proposalId)
    {
        var parliamentContractStub = GetParliamentContractTester(reviewer);
        var utcNow = TimestampHelper.GetUtcNow();
        BlockTimeProvider.SetBlockTime(utcNow);
        var transactionResult =
            await parliamentContractStub.Reject.SendAsync(proposalId);
        transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var receiptCreated = ReceiptCreated.Parser.ParseFrom(transactionResult.TransactionResult.Logs
            .FirstOrDefault(l => l.Name == nameof(ReceiptCreated))
            ?.NonIndexed);
        ValidateReceiptCreated(receiptCreated, Address.FromPublicKey(reviewer.PublicKey), proposalId, utcNow,
            nameof(parliamentContractStub.Reject));
    }

    private async Task AbstainAsync(ECKeyPair reviewer, Hash proposalId)
    {
        var parliamentContractStub = GetParliamentContractTester(reviewer);
        var utcNow = TimestampHelper.GetUtcNow();
        BlockTimeProvider.SetBlockTime(utcNow);
        var transactionResult =
            await parliamentContractStub.Abstain.SendAsync(proposalId);
        transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var receiptCreated = ReceiptCreated.Parser.ParseFrom(transactionResult.TransactionResult.Logs
            .FirstOrDefault(l => l.Name == nameof(ReceiptCreated))
            ?.NonIndexed);
        ValidateReceiptCreated(receiptCreated, Address.FromPublicKey(reviewer.PublicKey), proposalId, utcNow,
            nameof(parliamentContractStub.Abstain));
    }

    private void ValidateReceiptCreated(ReceiptCreated receiptCreated, Address sender, Hash proposalId,
        Timestamp blockTime, string receiptType)
    {
        receiptCreated.Address.ShouldBe(sender);
        receiptCreated.ProposalId.ShouldBe(proposalId);
        receiptCreated.Time.ShouldBe(blockTime);
        receiptCreated.ReceiptType.ShouldBe(receiptType);
    }

    private async Task<Hash> CreateFeeProposalAsync(Address contractAddress, Address organizationAddress,
        string methodName, IMessage input)
    {
        var parliamentContractStub = GetParliamentContractTester(InitialMinersKeyPairs[0]);
        var proposal = new CreateProposalInput
        {
            OrganizationAddress = organizationAddress,
            ContractMethodName = methodName,
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1),
            Params = input.ToByteString(),
            ToAddress = contractAddress
        };

        var createResult = await parliamentContractStub.CreateProposal.SendAsync(proposal);
        var proposalId = createResult.Output;

        return proposalId;
    }
}