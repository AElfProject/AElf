using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core.Extension;
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

namespace AElf.Contracts.Referendum;

public sealed class ReferendumContractTest : ReferendumContractTestBase
{
    private readonly IBlockchainService _blockchainService;
    private readonly TestDemoSmartContractAddressNameProvider _smartContractAddressNameProvider;
    private readonly ISmartContractAddressService _smartContractAddressService;

    public ReferendumContractTest()
    {
        _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
        _blockchainService = GetRequiredService<IBlockchainService>();
        _smartContractAddressNameProvider = GetRequiredService<TestDemoSmartContractAddressNameProvider>();
        InitializeContracts();
    }

    [Fact]
    public async Task Get_Organization_Test()
    {
        //not exist
        {
            var organization =
                await ReferendumContractStub.GetOrganization.CallAsync(Accounts[0].Address);
            organization.ShouldBe(new Organization());

            var result = await ReferendumContractStub.ValidateOrganizationExist.CallAsync(DefaultSender);
            result.Value.ShouldBeFalse();
        }

        var minimalApproveThreshold = 5000;
        var minimalVoteThreshold = 5000;
        var maximalRejectionThreshold = 10000;
        var maximalAbstentionThreshold = 10000;
        var createOrganizationInput = new CreateOrganizationInput
        {
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
            },
            TokenSymbol = "ELF"
        };
        var transactionResult =
            await ReferendumContractStub.CreateOrganization.SendAsync(createOrganizationInput);
        var organizationAddress = transactionResult.Output;
        var getOrganization = await ReferendumContractStub.GetOrganization.CallAsync(organizationAddress);

        getOrganization.OrganizationAddress.ShouldBe(organizationAddress);
        getOrganization.ProposalReleaseThreshold.MinimalApprovalThreshold.ShouldBe(minimalApproveThreshold);
        getOrganization.ProposalReleaseThreshold.MinimalVoteThreshold.ShouldBe(minimalVoteThreshold);
        getOrganization.ProposalReleaseThreshold.MaximalAbstentionThreshold.ShouldBe(maximalAbstentionThreshold);
        getOrganization.ProposalReleaseThreshold.MaximalRejectionThreshold.ShouldBe(maximalRejectionThreshold);
        getOrganization.OrganizationHash.ShouldBe(HashHelper.ComputeFrom(createOrganizationInput));
    }

    [Fact]
    public async Task Get_Proposal_Test()
    {
        //not exist
        {
            var proposal = await ReferendumContractStub.GetProposal.CallAsync(HashHelper.ComputeFrom("Test"));
            proposal.ShouldBe(new ProposalOutput());
        }

        var minimalApproveThreshold = 5000;
        var minimalVoteThreshold = 5000;
        var maximalRejectionThreshold = 10000;
        var maximalAbstentionThreshold = 10000;
        var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, new[] { DefaultSender });
        var createInput = new CreateInput
        {
            Symbol = "NEW",
            Decimals = 2,
            TotalSupply = 10_0000,
            TokenName = "new token",
            Issuer = organizationAddress,
            IsBurnable = true
        };
        var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
        var getProposal = await ReferendumContractStub.GetProposal.SendAsync(proposalId);

        getProposal.Output.Proposer.ShouldBe(DefaultSender);
        getProposal.Output.ContractMethodName.ShouldBe(nameof(TokenContract.Create));
        getProposal.Output.ProposalId.ShouldBe(proposalId);
        getProposal.Output.OrganizationAddress.ShouldBe(organizationAddress);
        getProposal.Output.ToAddress.ShouldBe(TokenContractAddress);
        getProposal.Output.Params.ShouldBe(createInput.ToByteString());
    }

    [Fact]
    public async Task Create_ProposalFailed_Test()
    {
        var minimalApproveThreshold = 5000;
        var minimalVoteThreshold = 5000;
        var maximalRejectionThreshold = 10000;
        var maximalAbstentionThreshold = 10000;
        var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, new[] { DefaultSender });
        var blockTime = BlockTimeProvider.GetBlockTime();

        {
            //"Invalid proposal."
            var createProposalInput = new CreateProposalInput
            {
                ToAddress = Accounts[0].Address,
                Params = ByteString.CopyFromUtf8("Test"),
                ExpiredTime = blockTime.AddDays(1),
                OrganizationAddress = organizationAddress
            };
            var transactionResult =
                await ReferendumContractStub.CreateProposal.SendWithExceptionAsync(createProposalInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
        }
        {
            var createProposalInput = new CreateProposalInput
            {
                ToAddress = null,
                Params = ByteString.CopyFromUtf8("Test"),
                ExpiredTime = blockTime.AddDays(1),
                OrganizationAddress = organizationAddress,
                ContractMethodName = "Test"
            };
            var transactionResult =
                await ReferendumContractStub.CreateProposal.SendWithExceptionAsync(createProposalInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
        }
        {
            var createProposalInput = new CreateProposalInput
            {
                ToAddress = Accounts[0].Address,
                Params = ByteString.CopyFromUtf8("Test"),
                ExpiredTime = null,
                OrganizationAddress = organizationAddress,
                ContractMethodName = "Test"
            };

            var transactionResult =
                await ReferendumContractStub.CreateProposal.SendWithExceptionAsync(createProposalInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
        }
        {
            //"Expired proposal."
            var createProposalInput = new CreateProposalInput
            {
                ToAddress = Accounts[0].Address,
                Params = ByteString.CopyFromUtf8("Test"),
                ExpiredTime = TimestampHelper.GetUtcNow().AddSeconds(-5),
                OrganizationAddress = organizationAddress,
                ContractMethodName = "Test"
            };

            BlockTimeProvider.SetBlockTime(TimestampHelper.GetUtcNow());

            var transactionResult =
                await ReferendumContractStub.CreateProposal.SendWithExceptionAsync(createProposalInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        }
        {
            //"No registered organization."
            var createProposalInput = new CreateProposalInput
            {
                ToAddress = Accounts[0].Address,
                Params = ByteString.CopyFromUtf8("Test"),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                OrganizationAddress = DefaultSender,
                ContractMethodName = "Test"
            };

            var transactionResult =
                await ReferendumContractStub.CreateProposal.SendWithExceptionAsync(createProposalInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("Organization not found.").ShouldBeTrue();
        }
        {
            //"Proposal with same input."
            var createProposalInput = new CreateProposalInput
            {
                ToAddress = Accounts[0].Address,
                Params = ByteString.CopyFromUtf8("Test"),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                OrganizationAddress = organizationAddress,
                ContractMethodName = "Test"
            };
            var transactionResult1 = await ReferendumContractStub.CreateProposal.SendAsync(createProposalInput);
            transactionResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var transactionResult2 = await ReferendumContractStub.CreateProposal.SendAsync(createProposalInput);
            transactionResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        {
            //"Proposal with invalid url."
            var createProposalInput = new CreateProposalInput
            {
                ToAddress = Accounts[0].Address,
                Params = ByteString.CopyFromUtf8("Test"),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                OrganizationAddress = organizationAddress,
                ContractMethodName = "Test",
                ProposalDescriptionUrl = "test.com"
            };
            var transactionResult =
                await ReferendumContractStub.CreateProposal.SendWithExceptionAsync(createProposalInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();

            createProposalInput.ProposalDescriptionUrl = "https://test.com/test%abcd%&wxyz";
            var transactionResult2 = await ReferendumContractStub.CreateProposal.SendAsync(createProposalInput);
            transactionResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        {
            // unauthorized to propose
            var createProposalInput = new CreateProposalInput
            {
                ToAddress = Accounts[0].Address,
                Params = ByteString.CopyFromUtf8("Test"),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                OrganizationAddress = organizationAddress,
                ContractMethodName = "Test"
            };
            var transactionResult = await GetReferendumContractTester(Accounts.Last().KeyPair)
                .CreateProposal.SendWithExceptionAsync(createProposalInput);
            transactionResult.TransactionResult.Error.ShouldContain("Unauthorized to propose.");
        }
    }

    [Fact]
    public async Task Approve_Proposal_NotFoundProposal_Test()
    {
        var transactionResult =
            await ReferendumContractStub.Approve.SendWithExceptionAsync(HashHelper.ComputeFrom("Test"));
        transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        transactionResult.TransactionResult.Error.Contains("Invalid proposal id").ShouldBeTrue();
    }

    [Fact]
    public async Task Approve_WithoutAllowance()
    {
        var minimalApproveThreshold = 5000;
        var minimalVoteThreshold = 5000;
        var maximalRejectionThreshold = 10000;
        var maximalAbstentionThreshold = 10000;
        var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, new[] { DefaultSender });
        var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
        ReferendumContractStub = GetReferendumContractTester(DefaultSenderKeyPair);
        var approveTx = await ReferendumContractStub.Approve.SendWithExceptionAsync(proposalId);
        approveTx.TransactionResult.Error.ShouldContain("Allowance not enough.");
    }

    [Fact]
    public async Task Approve_Success()
    {
        var minimalApproveThreshold = 5000;
        var minimalVoteThreshold = 5000;
        var maximalRejectionThreshold = 10000;
        var maximalAbstentionThreshold = 10000;
        var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, new[] { DefaultSender });
        var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
        ReferendumContractStub = GetReferendumContractTester(DefaultSenderKeyPair);
        var proposalVirtualAddress = await ReferendumContractStub.GetProposalVirtualAddress.CallAsync(proposalId);
        long amount = 1000;
        await TokenContractStub.Approve.SendAsync(new ApproveInput
        {
            Amount = amount,
            Spender = proposalVirtualAddress,
            Symbol = "ELF"
        });
        var balance1 = await GetBalanceAsync("ELF", DefaultSender);
        var transactionResult1 = await ReferendumContractStub.Approve.SendAsync(proposalId);
        transactionResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var balance2 = await GetBalanceAsync("ELF", DefaultSender);
        balance2.ShouldBe(balance1 - amount);

        var proposal = await ReferendumContractStub.GetProposal.CallAsync(proposalId);
        proposal.ApprovalCount.ShouldBe(amount);
    }

    [Fact]
    public async Task Approve_Proposal_MultiTimes_Test()
    {
        var minimalApproveThreshold = 5000;
        var minimalVoteThreshold = 5000;
        var maximalRejectionThreshold = 10000;
        var maximalAbstentionThreshold = 10000;
        var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, new[] { DefaultSender });
        var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);

        var keyPair = Accounts[1].KeyPair;
        var userBalanceBeforeApprove =
            await GetBalanceAsync("ELF", Address.FromPublicKey(keyPair.PublicKey));
        long amount = 1000;

        await ApproveAllowanceAsync(keyPair, amount, proposalId);
        var referendumContractStub = GetReferendumContractTester(keyPair);
        await referendumContractStub.Approve.SendAsync(proposalId);
        var userBalance =
            await GetBalanceAsync("ELF", Accounts[1].Address);
        userBalance.ShouldBe(userBalanceBeforeApprove - amount);

        var transactionResult2 = await referendumContractStub.Approve.SendWithExceptionAsync(proposalId);
        transactionResult2.TransactionResult.Error.ShouldContain("Allowance not enough.");

        await ApproveAllowanceAsync(keyPair, amount, proposalId);

        var result = await referendumContractStub.Reject.SendWithExceptionAsync(proposalId);
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        result.TransactionResult.Error.ShouldContain("Already locked.");
    }

    [Fact]
    public async Task Approve_Proposal_ExpiredTime_Test()
    {
        var minimalApproveThreshold = 5000;
        var minimalVoteThreshold = 5000;
        var maximalRejectionThreshold = 10000;
        var maximalAbstentionThreshold = 10000;
        var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, new[] { DefaultSender });
        var timeStamp = TimestampHelper.GetUtcNow();
        var proposalId =
            await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress, timeStamp.AddSeconds(5));

        var referendumContractStub = GetReferendumContractTester(Accounts[1].KeyPair);
        BlockTimeProvider.SetBlockTime(timeStamp.AddSeconds(10));

        var error = await referendumContractStub.Approve.CallWithExceptionAsync(proposalId);
        error.Value.ShouldContain("Invalid proposal.");
    }

    [Fact]
    public async Task Approve_WrongAllowance()
    {
        var minimalApproveThreshold = 5000;
        var minimalVoteThreshold = 5000;
        var maximalRejectionThreshold = 10000;
        var maximalAbstentionThreshold = 10000;
        var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, new[] { DefaultSender });
        var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);

        var keyPair = Accounts[1].KeyPair;
        long amount = 1000;
        await GetTokenContractTester(keyPair).Approve.SendAsync(new ApproveInput
        {
            Amount = amount,
            Spender = ReferendumContractAddress,
            Symbol = "ELF"
        });
        ReferendumContractStub = GetReferendumContractTester(keyPair);
        var approveTransaction = await ReferendumContractStub.Approve.SendWithExceptionAsync(proposalId);
        approveTransaction.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        approveTransaction.TransactionResult.Error.Contains("Allowance not enough.").ShouldBeTrue();
    }

    [Fact]
    public async Task ReclaimVoteToken_AfterRelease()
    {
        var minimalApproveThreshold = 5000;
        var minimalVoteThreshold = 5000;
        var maximalRejectionThreshold = 10000;
        var maximalAbstentionThreshold = 10000;
        var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, new[] { DefaultSender });
        await ApproveAndTransferCreateTokenFee(DefaultSenderKeyPair, minimalApproveThreshold, organizationAddress);
        var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
        var amount = 5000;
        var accountApprove = Accounts[3];
        await ApproveAllowanceAsync(accountApprove.KeyPair, amount, proposalId);
        var balanceApprove1 = await GetBalanceAsync("ELF", accountApprove.Address);
        var accountReject = Accounts[4];
        await ApproveAllowanceAsync(accountReject.KeyPair, amount, proposalId);
        var balanceReject1 = await GetBalanceAsync("ELF", accountReject.Address);
        var accountAbstain = Accounts[5];
        await ApproveAllowanceAsync(accountAbstain.KeyPair, amount, proposalId);
        var balanceAbstain1 = await GetBalanceAsync("ELF", accountAbstain.Address);

        await ApproveAsync(accountApprove.KeyPair, proposalId);
        await RejectAsync(accountReject.KeyPair, proposalId);
        await AbstainAsync(accountAbstain.KeyPair, proposalId);

        ReferendumContractStub = GetReferendumContractTester(DefaultSenderKeyPair);
        var result = await ReferendumContractStub.Release.SendAsync(proposalId);
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var referendumContractStubApprove = GetReferendumContractTester(accountApprove.KeyPair);
        var reclaimResult1 = await referendumContractStubApprove.ReclaimVoteToken.SendAsync(proposalId);
        reclaimResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var referendumContractStubReject = GetReferendumContractTester(accountReject.KeyPair);
        var reclaimResult2 = await referendumContractStubReject.ReclaimVoteToken.SendAsync(proposalId);
        reclaimResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var referendumContractStubAbstain = GetReferendumContractTester(accountAbstain.KeyPair);
        var reclaimResult3 = await referendumContractStubAbstain.ReclaimVoteToken.SendAsync(proposalId);
        reclaimResult3.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var balanceApprove2 = await GetBalanceAsync("ELF", accountApprove.Address);
        balanceApprove2.ShouldBe(balanceApprove1);
        var balanceReject2 = await GetBalanceAsync("ELF", accountReject.Address);
        balanceReject2.ShouldBe(balanceReject1);
        var balanceAbstain2 = await GetBalanceAsync("ELF", accountAbstain.Address);
        balanceAbstain2.ShouldBe(balanceAbstain1);
    }

    [Fact]
    public async Task ReclaimVoteToken_AfterExpired()
    {
        var minimalApproveThreshold = 5000;
        var minimalVoteThreshold = 5000;
        var maximalRejectionThreshold = 10000;
        var maximalAbstentionThreshold = 10000;
        var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, new[] { DefaultSender });
        var timeStamp = TimestampHelper.GetUtcNow();
        BlockTimeProvider.SetBlockTime(timeStamp); // set next block time
        var proposalId =
            await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress, timeStamp.AddSeconds(5));
        var amount = 5000;
        var account = Accounts[3];
        await ApproveAllowanceAsync(account.KeyPair, amount, proposalId);
        var balance1 = await GetBalanceAsync("ELF", account.Address);

        await ApproveAsync(account.KeyPair, proposalId);
        BlockTimeProvider.SetBlockTime(timeStamp.AddSeconds(10)); // set next block time
        var referendumContractStubApprove = GetReferendumContractTester(account.KeyPair);
        var reclaimResult = await referendumContractStubApprove.ReclaimVoteToken.SendAsync(proposalId);
        reclaimResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var balance2 = await GetBalanceAsync("ELF", account.Address);
        balance2.ShouldBe(balance1);

        //delete expired proposal
        var clearResult = await ReferendumContractStub.ClearProposal.SendAsync(proposalId);
        clearResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var proposal = await ReferendumContractStub.GetProposal.CallAsync(proposalId);
        proposal.ShouldBe(new ProposalOutput());
    }

    [Fact]
    public async Task ReclaimVoteToken_WithoutRelease()
    {
        var minimalApproveThreshold = 5000;
        var minimalVoteThreshold = 5000;
        var maximalRejectionThreshold = 10000;
        var maximalAbstentionThreshold = 10000;
        var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, new[] { DefaultSender });
        var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);

        var account = Accounts[1];
        var amount = 2000;
        await ApproveAllowanceAsync(account.KeyPair, amount, proposalId);
        await ApproveAsync(account.KeyPair, proposalId);

        var reclaimResult = await GetReferendumContractTester(account.KeyPair).ReclaimVoteToken
            .SendWithExceptionAsync(proposalId);
        reclaimResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        reclaimResult.TransactionResult.Error.Contains("Unable to reclaim at this time.").ShouldBeTrue();
    }

    [Fact]
    public async Task Reclaim_VoteTokenWithoutVote_Test()
    {
        var minimalApproveThreshold = 5000;
        var minimalVoteThreshold = 5000;
        var maximalRejectionThreshold = 10000;
        var maximalAbstentionThreshold = 10000;
        var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, new[] { DefaultSender });
        var timeStamp = TimestampHelper.GetUtcNow();
        var proposalId =
            await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);

        var referendumContractStub = GetReferendumContractTester(Accounts[1].KeyPair);
        BlockTimeProvider.SetBlockTime(timeStamp.AddDays(1)); // set next block time

        var reclaimResult = await referendumContractStub.ReclaimVoteToken.SendWithExceptionAsync(proposalId);
        reclaimResult.TransactionResult.Error.ShouldContain("Nothing to reclaim.");
    }

    [Fact]
    public async Task Check_Proposal_ToBeRelease()
    {
        var minimalApproveThreshold = 1000;
        var minimalVoteThreshold = 1000;
        var maximalRejectionThreshold = 1000;
        var maximalAbstentionThreshold = 1000;
        var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, new[] { DefaultSender });
        //Abstain probability >= maximalAbstentionThreshold
        {
            var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
            var keyPair1 = Accounts[1].KeyPair;
            var amount = 1001;
            await ApproveAllowanceAsync(keyPair1, amount, proposalId);
            await ApproveAsync(keyPair1, proposalId);
            var result = await ReferendumContractStub.GetProposal.CallAsync(proposalId);
            result.ToBeReleased.ShouldBeTrue();
            var keyPair2 = Accounts[2].KeyPair;
            await ApproveAllowanceAsync(keyPair2, amount, proposalId);
            await AbstainAsync(keyPair2, proposalId);
            result = await ReferendumContractStub.GetProposal.CallAsync(proposalId);
            result.ToBeReleased.ShouldBeFalse();
        }
        //Rejection probability > maximalRejectionThreshold
        {
            var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
            var keyPair1 = Accounts[1].KeyPair;
            var amount = 1001;
            await ApproveAllowanceAsync(keyPair1, amount, proposalId);
            await ApproveAsync(keyPair1, proposalId);
            var result = await ReferendumContractStub.GetProposal.CallAsync(proposalId);
            result.ToBeReleased.ShouldBeTrue();
            var keyPair2 = Accounts[2].KeyPair;
            await ApproveAllowanceAsync(keyPair2, amount, proposalId);
            await RejectAsync(keyPair2, proposalId);
            result = await ReferendumContractStub.GetProposal.CallAsync(proposalId);
            result.ToBeReleased.ShouldBeFalse();
        }
    }

    [Fact]
    public async Task Release_NotEnoughWeight_Test()
    {
        var minimalApproveThreshold = 5000;
        var minimalVoteThreshold = 5000;
        var maximalRejectionThreshold = 10000;
        var maximalAbstentionThreshold = 10000;
        var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, new[] { DefaultSender });
        var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
        var keyPair = Accounts[3].KeyPair;
        var amount = 2000;
        await ApproveAllowanceAsync(keyPair, amount, proposalId);
        await ApproveAsync(keyPair, proposalId);

        ReferendumContractStub = GetReferendumContractTester(DefaultSenderKeyPair);
        var result = await ReferendumContractStub.Release.SendWithExceptionAsync(proposalId);
        //Reviewer Shares < ReleaseThreshold, release failed
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        result.TransactionResult.Error.Contains("Not approved.").ShouldBeTrue();
    }

    [Fact]
    public async Task Release_NotFound_Test()
    {
        var proposalId = HashHelper.ComputeFrom("test");
        var result = await ReferendumContractStub.Release.SendWithExceptionAsync(proposalId);
        //Proposal not found
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        result.TransactionResult.Error.ShouldContain("Invalid proposal id.");
    }

    [Fact]
    public async Task Release_WrongSender_Test()
    {
        var minimalApproveThreshold = 5000;
        var minimalVoteThreshold = 5000;
        var maximalRejectionThreshold = 10000;
        var maximalAbstentionThreshold = 10000;
        var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, new[] { DefaultSender });
        var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
        var keyPair = Accounts[3].KeyPair;
        var amount = 5000;
        await ApproveAllowanceAsync(keyPair, amount, proposalId);
        await ApproveAsync(keyPair, proposalId);

        var referendumContractStub = GetReferendumContractTester(keyPair);
        var result = await referendumContractStub.Release.SendWithExceptionAsync(proposalId);
        result.TransactionResult.Error.Contains("No permission.").ShouldBeTrue();
    }

    [Fact]
    public async Task Release_Proposal_Test()
    {
        var minimalApproveThreshold = 5000;
        var minimalVoteThreshold = 5000;
        var maximalRejectionThreshold = 10000;
        var maximalAbstentionThreshold = 10000;
        var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, new[] { DefaultSender });
        
        await ApproveAndTransferCreateTokenFee(DefaultSenderKeyPair, minimalApproveThreshold, organizationAddress);
        var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
        await ApproveAllowanceAsync(Accounts[3].KeyPair, minimalApproveThreshold, proposalId);

        await ApproveAsync(Accounts[3].KeyPair, proposalId);

        ReferendumContractStub = GetReferendumContractTester(DefaultSenderKeyPair);
        var result = await ReferendumContractStub.Release.SendAsync(proposalId);
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        // Check inline transaction result
        var newToken = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput { Symbol = "NEW" });
        newToken.Issuer.ShouldBe(organizationAddress);
    }

    [Fact]
    public async Task Release_Proposal_AlreadyReleased_Test()
    {
        var minimalApproveThreshold = 5000;
        var minimalVoteThreshold = 5000;
        var maximalRejectionThreshold = 10000;
        var maximalAbstentionThreshold = 10000;
        var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, new[] { DefaultSender });
        
        await ApproveAndTransferCreateTokenFee(DefaultSenderKeyPair, minimalApproveThreshold, organizationAddress);
        var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);

        // do not reach minimal approve amount
        {
            var result = await ReferendumContractStub.Release.SendWithExceptionAsync(proposalId);
            result.TransactionResult.Error.ShouldContain("Not approved");
        }

        {
            var amount = 5000;
            var keyPair = Accounts[3].KeyPair;
            await ApproveAllowanceAsync(keyPair, amount, proposalId);
            await ApproveAsync(keyPair, proposalId);

            var referendumContractStub = GetReferendumContractTester(DefaultSenderKeyPair);
            var result = await referendumContractStub.Release.SendAsync(proposalId);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var proposalReleased = ProposalReleased.Parser.ParseFrom(result.TransactionResult.Logs
                    .First(l => l.Name.Contains(nameof(ProposalReleased))).NonIndexed)
                .ProposalId;
            proposalReleased.ShouldBe(proposalId);

            //After release,the proposal will be deleted
            var getProposal = await ReferendumContractStub.GetProposal.CallAsync(proposalId);
            getProposal.ShouldBe(new ProposalOutput());
        }
        {
            var amount = 5000;
            var keyPair = Accounts[3].KeyPair;
            await ApproveAllowanceAsync(keyPair, amount, proposalId);

            //approve the same proposal again
            var referendumContractStub = GetReferendumContractTester(keyPair);
            var transactionResult = await referendumContractStub.Approve.SendWithExceptionAsync(proposalId);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.ShouldContain("Invalid proposal");

            //release the same proposal again
            var anotherReferendumContractStub = GetReferendumContractTester(DefaultSenderKeyPair);
            var transactionResult2 =
                await anotherReferendumContractStub.Release.SendWithExceptionAsync(proposalId);
            transactionResult2.TransactionResult.Error.ShouldContain("Invalid proposal id.");
        }
    }

    [Fact]
    public async Task Change_OrganizationThreshold_Test()
    {
        var minimalApproveThreshold = 5000;
        var minimalVoteThreshold = 5000;
        var maximalRejectionThreshold = 10000;
        var maximalAbstentionThreshold = 10000;
        var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, new[] { DefaultSender });
        var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
        var keyPair = Accounts[3].KeyPair;
        await ApproveAllowanceAsync(keyPair, minimalApproveThreshold, proposalId);
        await ApproveAsync(Accounts[3].KeyPair, proposalId);
        var proposal = await ReferendumContractStub.GetProposal.CallAsync(proposalId);
        proposal.ToBeReleased.ShouldBeTrue();

        // invalid sender
        {
            var ret =
                await ReferendumContractStub.ChangeOrganizationThreshold.SendWithExceptionAsync(
                    new ProposalReleaseThreshold());
            ret.TransactionResult.Error.ShouldContain("Organization not found");
        }

        {
            var proposalReleaseThresholdInput = new ProposalReleaseThreshold
            {
                MinimalVoteThreshold = 20000
            };

            var changeProposalId = await CreateReferendumProposalAsync(DefaultSenderKeyPair,
                proposalReleaseThresholdInput,
                nameof(ReferendumContractStub.ChangeOrganizationThreshold), organizationAddress,
                ReferendumContractAddress);
            await ApproveAllowanceAsync(keyPair, minimalApproveThreshold, changeProposalId);
            await ApproveAsync(Accounts[3].KeyPair, changeProposalId);
            var referendumContractStub = GetReferendumContractTester(DefaultSenderKeyPair);
            var result = await referendumContractStub.Release.SendWithExceptionAsync(changeProposalId);
            result.TransactionResult.Error.ShouldContain("Invalid organization.");
        }

        {
            var proposalReleaseThresholdInput = new ProposalReleaseThreshold
            {
                MinimalVoteThreshold = 20000,
                MinimalApprovalThreshold = minimalApproveThreshold
            };

            ReferendumContractStub = GetReferendumContractTester(DefaultSenderKeyPair);
            var changeProposalId = await CreateReferendumProposalAsync(DefaultSenderKeyPair,
                proposalReleaseThresholdInput,
                nameof(ReferendumContractStub.ChangeOrganizationThreshold), organizationAddress,
                ReferendumContractAddress);
            await ApproveAllowanceAsync(keyPair, minimalApproveThreshold, changeProposalId);
            await ApproveAsync(Accounts[3].KeyPair, changeProposalId);
            var referendumContractStub = GetReferendumContractTester(DefaultSenderKeyPair);
            var result = await referendumContractStub.Release.SendAsync(changeProposalId);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            proposal = await referendumContractStub.GetProposal.CallAsync(proposalId);
            proposal.ToBeReleased.ShouldBeFalse();
        }
    }

    [Fact]
    public async Task ChangeOrganizationProposalWhitelist_Fail_Test()
    {
        // invalid sender
        {
            var ret =
                await ReferendumContractStub.ChangeOrganizationProposerWhiteList.SendWithExceptionAsync(
                    new ProposerWhiteList());
            ret.TransactionResult.Error.ShouldContain("Organization not found");
        }

        // invalid proposal whitelist
        {
            var organizationAddress = await CreateOrganizationAsync();
            var newProposalWhitelist = new ProposerWhiteList();
            var changeProposerWhitelistProposalId = await CreateReferendumProposalAsync(DefaultSenderKeyPair,
                newProposalWhitelist,
                nameof(ReferendumContractStub.ChangeOrganizationProposerWhiteList), organizationAddress,
                ReferendumContractAddress);
            var keyPair = Accounts[3].KeyPair;
            await ApproveAllowanceAsync(keyPair, 5000, changeProposerWhitelistProposalId);
            await ApproveAsync(keyPair, changeProposerWhitelistProposalId);
            var ret = await ReferendumContractStub.Release.SendWithExceptionAsync(
                changeProposerWhitelistProposalId);
            ret.TransactionResult.Error.ShouldContain("Invalid organization");
        }
    }

    [Fact]
    public async Task Change_OrganizationProposalWhitelist_Test()
    {
        var minimalApproveThreshold = 5000;
        var minimalVoteThreshold = 5000;
        var maximalRejectionThreshold = 10000;
        var maximalAbstentionThreshold = 10000;
        var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, new[] { DefaultSender });

        var whiteAddress = Accounts[3].Address;
        var proposerWhiteList = new ProposerWhiteList
        {
            Proposers = { whiteAddress }
        };

        ReferendumContractStub = GetReferendumContractTester(DefaultSenderKeyPair);
        var changeProposalId = await CreateReferendumProposalAsync(DefaultSenderKeyPair, proposerWhiteList,
            nameof(ReferendumContractStub.ChangeOrganizationProposerWhiteList), organizationAddress,
            ReferendumContractAddress);
        await ApproveAllowanceAsync(Accounts[3].KeyPair, minimalApproveThreshold, changeProposalId);
        await ApproveAsync(Accounts[3].KeyPair, changeProposalId);
        ReferendumContractStub = GetReferendumContractTester(DefaultSenderKeyPair);
        var releaseResult = await ReferendumContractStub.Release.SendAsync(changeProposalId);
        releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        //verify whiteList
        var verifyResult = await ReferendumContractStub.ValidateProposerInWhiteList.CallAsync(
            new ValidateProposerInWhiteListInput
            {
                OrganizationAddress = organizationAddress,
                Proposer = whiteAddress
            });
        verifyResult.Value.ShouldBeTrue();

        var timeStamp = TimestampHelper.GetUtcNow();
        var createInput = new CreateInput
        {
            Symbol = "NEW",
            Decimals = 2,
            TotalSupply = 10_0000,
            TokenName = "new token",
            Issuer = organizationAddress,
            IsBurnable = true
        };
        var createProposalInput = new CreateProposalInput
        {
            ContractMethodName = nameof(TokenContract.Create),
            ToAddress = TokenContractAddress,
            Params = createInput.ToByteString(),
            ExpiredTime = timeStamp ?? BlockTimeProvider.GetBlockTime().AddSeconds(1000),
            OrganizationAddress = organizationAddress
        };
        ReferendumContractStub = GetReferendumContractTester(DefaultSenderKeyPair);
        var result = await ReferendumContractStub.CreateProposal.SendWithExceptionAsync(createProposalInput);
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        result.TransactionResult.Error.ShouldContain("Unauthorized to propose.");
    }

    [Fact]
    public async Task SetMethodFee_Fail_Test()
    {
        // invalid token symbol
        {
            var invalidTokenSymbol = "CPW";
            var inputFee = GetValidMethodFees();
            inputFee.Fees[0].Symbol = invalidTokenSymbol;
            var ret = await ReferendumContractStub.SetMethodFee.SendWithExceptionAsync(inputFee);
            ret.TransactionResult.Error.ShouldContain("Token is not found");
        }

        //invalid fee amount
        {
            var inputFee = GetValidMethodFees();
            inputFee.Fees[0].BasicFee = -1;
            var ret = await ReferendumContractStub.SetMethodFee.SendWithExceptionAsync(inputFee);
            ret.TransactionResult.Error.ShouldContain("Invalid amount");
        }

        //invalid sender
        {
            var inputFee = GetValidMethodFees();
            var ret = await ReferendumContractStub.SetMethodFee.SendWithExceptionAsync(inputFee);
            ret.TransactionResult.Error.ShouldContain("Unauthorized");
        }
    }

    [Fact]
    public async Task SetMethodFee_Test()
    {
        var inputFee = new MethodFees
        {
            MethodName = nameof(ReferendumContractStub.CreateProposal),
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
        var result = await ParliamentContractStub.CreateProposal.SendAsync(new CreateProposalInput
        {
            ToAddress = ReferendumContractAddress,
            Params = inputFee.ToByteString(),
            ContractMethodName = nameof(ReferendumContractStub.SetMethodFee),
            ExpiredTime = TimestampHelper.GetUtcNow().AddHours(1),
            OrganizationAddress = defaultOrganization
        });
        var proposalId = result.Output;
        await ApproveWithMinersAsync(proposalId);
        await ParliamentContractStub.Release.SendAsync(proposalId);

        var feeResult = await ReferendumContractStub.GetMethodFee.CallAsync(new StringValue
        {
            Value = nameof(ReferendumContractStub.CreateProposal)
        });
        feeResult.Fees.First().ShouldBe(new MethodFee
        {
            Symbol = "ELF",
            BasicFee = 5000_0000L
        });
    }

    [Fact]
    public async Task ChangeMethodFeeController_With_Invalid_Organization_Test()
    {
        var methodFeeController = await ReferendumContractStub.GetMethodFeeController.CallAsync(new Empty());
        var proposalId = await CreateFeeProposalAsync(ReferendumContractAddress,
            methodFeeController.OwnerAddress, nameof(ReferendumContractStub.ChangeMethodFeeController),
            new AuthorityInfo
            {
                OwnerAddress = TokenContractAddress,
                ContractAddress = ParliamentContractAddress
            });
        await ApproveWithMinersAsync(proposalId);
        var ret = await ParliamentContractStub.Release.SendWithExceptionAsync(proposalId);
        ret.TransactionResult.Error.ShouldContain("Invalid authority input");
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

        var methodFeeController = await ReferendumContractStub.GetMethodFeeController.CallAsync(new Empty());
        var defaultOrganization = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
        methodFeeController.OwnerAddress.ShouldBe(defaultOrganization);

        const string proposalCreationMethodName = nameof(ReferendumContractStub.ChangeMethodFeeController);
        var proposalId = await CreateFeeProposalAsync(ReferendumContractAddress,
            methodFeeController.OwnerAddress, proposalCreationMethodName, new AuthorityInfo
            {
                OwnerAddress = organizationAddress,
                ContractAddress = ParliamentContractAddress
            });
        await ApproveWithMinersAsync(proposalId);
        var releaseResult = await ParliamentContractStub.Release.SendAsync(proposalId);
        releaseResult.TransactionResult.Error.ShouldBeNullOrEmpty();
        releaseResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var newMethodFeeController = await ReferendumContractStub.GetMethodFeeController.CallAsync(new Empty());
        newMethodFeeController.OwnerAddress.ShouldBe(organizationAddress);
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
        var result = await ReferendumContractStub.ChangeMethodFeeController.SendWithExceptionAsync(new AuthorityInfo
        {
            OwnerAddress = organizationAddress,
            ContractAddress = ParliamentContractAddress
        });

        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        result.TransactionResult.Error.Contains("Unauthorized behavior.").ShouldBeTrue();
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
            TokenSymbol = "ELF",
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
            await ReferendumContractStub.CalculateOrganizationAddress.SendAsync(createOrganizationInput);
        var transactionResult =
            await ReferendumContractStub.CreateOrganizationBySystemContract.SendWithExceptionAsync(input);
        transactionResult.TransactionResult.Error.ShouldContain("Unauthorized");
        //success
        var chain = await _blockchainService.GetChainAsync();
        var blockIndex = new BlockIndex
        {
            BlockHash = chain.BestChainHash,
            BlockHeight = chain.BestChainHeight
        };
        await _smartContractAddressService.SetSmartContractAddressAsync(blockIndex,
            _smartContractAddressNameProvider.ContractStringName, DefaultSender);
        var transactionResult1 =
            await ReferendumContractStub.CreateOrganizationBySystemContract.SendAsync(input);
        transactionResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        transactionResult1.Output.ShouldBe(addressByCalculate.Output);
        var boolResult =
            ReferendumContractStub.ValidateOrganizationExist.SendAsync(addressByCalculate.Output);
        boolResult.Result.Output.Value.ShouldBeTrue();
        //create again to verify address 
        var existTransactionResult =
            await ReferendumContractStub.CreateOrganizationBySystemContract.SendAsync(input);
        existTransactionResult.Output.ShouldBe(addressByCalculate.Output);
        //invalid contract
        var method = "OrganizationAddressFeedbackMethodName";
        var input2 = new CreateOrganizationBySystemContractInput
        {
            OrganizationCreationInput = createOrganizationInput,
            OrganizationAddressFeedbackMethod = method
        };
        var transactionResult2 =
            await ReferendumContractStub.CreateOrganizationBySystemContract.SendWithExceptionAsync(input2);
        transactionResult2.TransactionResult.Error.ShouldContain("invalid contract");
        //Invalid organization data
        var createOrganizationInput3 = new CreateOrganizationInput
        {
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
        var input3 = new CreateOrganizationBySystemContractInput
        {
            OrganizationCreationInput = createOrganizationInput3,
            OrganizationAddressFeedbackMethod = ""
        };
        var transactionResult3 =
            await ReferendumContractStub.CreateOrganizationBySystemContract.SendWithExceptionAsync(input3);
        transactionResult3.TransactionResult.Error.ShouldContain("Invalid organization data");
    }

    [Fact]
    public async Task CreateProposalBySystemContract_Test()
    {
        var minimalApproveThreshold = 2;
        var minimalVoteThreshold = 3;
        var maximalAbstentionThreshold = 1;
        var maximalRejectionThreshold = 1;
        var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, new[] { DefaultSender });
        var transferInput = new TransferInput
        {
            Symbol = "ELF",
            Amount = 100,
            To = TokenContractAddress,
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
        var chain = await _blockchainService.GetChainAsync();
        var blockIndex = new BlockIndex
        {
            BlockHash = chain.BestChainHash,
            BlockHeight = chain.BestChainHeight
        };
        //Unauthorized to propose
        var transactionResult =
            await ReferendumContractStub.CreateProposalBySystemContract.SendWithExceptionAsync(input);
        transactionResult.TransactionResult.Error.ShouldContain("Not authorized to propose.");
        //success
        await _smartContractAddressService.SetSmartContractAddressAsync(blockIndex,
            _smartContractAddressNameProvider.ContractStringName, DefaultSender);
        var transactionResult2 =
            await ReferendumContractStub.CreateProposalBySystemContract.SendAsync(input);
        transactionResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }

    [Fact]
    public async Task CreateOrganization_With_Invalid_Input_Test()
    {
        // token symbol is null or empty
        {
            var validInput = GetValidCreateOrganizationInput();
            validInput.TokenSymbol = string.Empty;
            var ret = await ReferendumContractStub.CreateOrganization.SendWithExceptionAsync(validInput);
            ret.TransactionResult.Error.ShouldContain("Invalid organization data");
        }

        // no proposer in proposeWhiteList
        {
            var validInput = GetValidCreateOrganizationInput();
            validInput.ProposerWhiteList.Proposers.Clear();
            var ret = await ReferendumContractStub.CreateOrganization.SendWithExceptionAsync(validInput);
            ret.TransactionResult.Error.ShouldContain("Invalid organization data");
        }

        //MinimalApprovalThreshold > MinimalVoteThreshold
        {
            var validInput = GetValidCreateOrganizationInput();
            validInput.ProposalReleaseThreshold.MinimalApprovalThreshold =
                validInput.ProposalReleaseThreshold.MinimalVoteThreshold + 1;
            var ret = await ReferendumContractStub.CreateOrganization.SendWithExceptionAsync(validInput);
            ret.TransactionResult.Error.ShouldContain("Invalid organization data");
        }

        //MinimalApprovalThreshold == 0
        {
            var validInput = GetValidCreateOrganizationInput();
            validInput.ProposalReleaseThreshold.MinimalApprovalThreshold = 0;
            var ret = await ReferendumContractStub.CreateOrganization.SendWithExceptionAsync(validInput);
            ret.TransactionResult.Error.ShouldContain("Invalid organization data");
        }

        //MaximalAbstentionThreshold < 0
        {
            var validInput = GetValidCreateOrganizationInput();
            validInput.ProposalReleaseThreshold.MaximalAbstentionThreshold = -1;
            var ret = await ReferendumContractStub.CreateOrganization.SendWithExceptionAsync(validInput);
            ret.TransactionResult.Error.ShouldContain("Invalid organization data");
        }

        //MaximalRejectionThreshold < 0
        {
            var validInput = GetValidCreateOrganizationInput();
            validInput.ProposalReleaseThreshold.MaximalRejectionThreshold = -1;
            var ret = await ReferendumContractStub.CreateOrganization.SendWithExceptionAsync(validInput);
            ret.TransactionResult.Error.ShouldContain("Invalid organization data");
        }
    }

    [Fact]
    public async Task CreateProposalBySystemContract_With_Invalid_Proposal_Test()
    {
        var minimalApproveThreshold = 2;
        var minimalVoteThreshold = 3;
        var maximalAbstentionThreshold = 1;
        var maximalRejectionThreshold = 1;
        var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
            maximalAbstentionThreshold, maximalRejectionThreshold, new[] { DefaultSender });
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

        //invalid organization
        {
            var proposal = GetValidCreateProposalInput(organizationAddress);
            proposal.OrganizationAddress = ReferendumContractAddress;
            input.ProposalInput = proposal;
            var ret =
                await ReferendumContractStub.CreateProposalBySystemContract.SendWithExceptionAsync(input);
            ret.TransactionResult.Error.ShouldContain("Organization not found");
        }

        //method name = string.Empty
        {
            var proposal = GetValidCreateProposalInput(organizationAddress);
            proposal.ContractMethodName = string.Empty;
            input.ProposalInput = proposal;
            var ret =
                await ReferendumContractStub.CreateProposalBySystemContract.SendWithExceptionAsync(input);
            ret.TransactionResult.Error.ShouldContain("Invalid proposal");
        }

        //invalid expire time
        {
            var proposal = GetValidCreateProposalInput(organizationAddress);
            proposal.ExpiredTime = new Timestamp();
            input.ProposalInput = proposal;
            var ret =
                await ReferendumContractStub.CreateProposalBySystemContract.SendWithExceptionAsync(input);
            ret.TransactionResult.Error.ShouldContain("Invalid proposal");
        }

        //invalid url
        {
            var proposal = GetValidCreateProposalInput(organizationAddress);
            proposal.ProposalDescriptionUrl = "ppp";
            input.ProposalInput = proposal;
            var ret =
                await ReferendumContractStub.CreateProposalBySystemContract.SendWithExceptionAsync(input);
            ret.TransactionResult.Error.ShouldContain("Invalid proposal");
        }
    }

    [Fact]
    public async Task ClearProposal_Fail_Test()
    {
        // the proposal that is not exist
        {
            var proposalId = new Hash();
            var ret = await ReferendumContractStub.ClearProposal.SendWithExceptionAsync(proposalId);
            ret.TransactionResult.Error.ShouldContain("Proposal clear failed");
        }

        // the proposal that is not expired
        {
            var organizationAddress = await CreateOrganizationAsync();
            var newProposalWhitelist = new ProposerWhiteList();
            var changeProposerWhitelistProposalId = await CreateReferendumProposalAsync(DefaultSenderKeyPair,
                newProposalWhitelist,
                nameof(ReferendumContractStub.ChangeOrganizationProposerWhiteList), organizationAddress,
                ReferendumContractAddress);
            var ret = await ReferendumContractStub.ClearProposal.SendWithExceptionAsync(
                changeProposerWhitelistProposalId);
            ret.TransactionResult.Error.ShouldContain("Proposal clear failed");
        }
    }

    private CreateOrganizationInput GetValidCreateOrganizationInput(Address sender = null)
    {
        var validSender = sender ?? DefaultSender;
        return new CreateOrganizationInput
        {
            ProposalReleaseThreshold = new ProposalReleaseThreshold
            {
                MinimalApprovalThreshold = 5000,
                MinimalVoteThreshold = 5000,
                MaximalAbstentionThreshold = 10000,
                MaximalRejectionThreshold = 10000
            },
            ProposerWhiteList = new ProposerWhiteList
            {
                Proposers = { validSender }
            },
            TokenSymbol = "ELF"
        };
    }

    private CreateProposalInput GetValidCreateProposalInput(Address organizationAddress)
    {
        var transferInput = new TransferInput
        {
            Symbol = "ELF",
            Amount = 100,
            To = TokenContractAddress,
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

    private async Task<Hash> CreateProposalAsync(ECKeyPair proposalKeyPair, Address organizationAddress,
        Timestamp timestamp = null)
    {
        var createInput = new CreateInput
        {
            Symbol = "NEW",
            Decimals = 2,
            TotalSupply = 10_0000,
            TokenName = "new token",
            Issuer = organizationAddress,
            IsBurnable = true
        };
        var createProposalInput = new CreateProposalInput
        {
            ContractMethodName = nameof(TokenContract.Create),
            ToAddress = TokenContractAddress,
            Params = createInput.ToByteString(),
            ExpiredTime = timestamp ?? BlockTimeProvider.GetBlockTime().AddSeconds(1000),
            OrganizationAddress = organizationAddress
        };
        ReferendumContractStub = GetReferendumContractTester(proposalKeyPair);
        var proposal = await ReferendumContractStub.CreateProposal.SendAsync(createProposalInput);
        proposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        return proposal.Output;
    }

    private async Task<Hash> CreateReferendumProposalAsync(ECKeyPair proposalKeyPair, IMessage input,
        string method, Address organizationAddress, Address toAddress)
    {
        var referendumContractStub = GetReferendumContractTester(proposalKeyPair);
        var createProposalInput = new CreateProposalInput
        {
            ContractMethodName = method,
            ToAddress = toAddress,
            Params = input.ToByteString(),
            ExpiredTime = BlockTimeProvider.GetBlockTime().AddDays(2),
            OrganizationAddress = organizationAddress
        };
        var proposal = await referendumContractStub.CreateProposal.SendAsync(createProposalInput);
        var proposalCreated = ProposalCreated.Parser.ParseFrom(proposal.TransactionResult.Logs
                .First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
            .ProposalId;
        proposal.Output.ShouldBe(proposalCreated);

        return proposal.Output;
    }

    private async Task<Address> CreateOrganizationAsync(long minimalApproveThreshold = 5000,
        long minimalVoteThreshold = 5000,
        long maximalAbstentionThreshold = 10000, long maximalRejectionThreshold = 10000,
        Address[] proposerWhiteList = null,
        string symbol = "ELF")
    {
        if (proposerWhiteList == null)
            proposerWhiteList = new[] { DefaultSender };
        var createOrganizationInput = new CreateOrganizationInput
        {
            ProposalReleaseThreshold = new ProposalReleaseThreshold
            {
                MinimalApprovalThreshold = minimalApproveThreshold,
                MinimalVoteThreshold = minimalVoteThreshold,
                MaximalAbstentionThreshold = maximalAbstentionThreshold,
                MaximalRejectionThreshold = maximalRejectionThreshold
            },
            TokenSymbol = symbol,
            ProposerWhiteList = new ProposerWhiteList
            {
                Proposers = { proposerWhiteList }
            }
        };
        var transactionResult =
            await ReferendumContractStub.CreateOrganization.SendAsync(createOrganizationInput);
        transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        return transactionResult.Output;
    }

    private async Task ApproveAsync(ECKeyPair reviewer, Hash proposalId)
    {
        var referendumContractStub = GetReferendumContractTester(reviewer);
        var utcNow = TimestampHelper.GetUtcNow();
        BlockTimeProvider.SetBlockTime(utcNow);
        var transactionResult = await referendumContractStub.Approve.SendAsync(proposalId);
        transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var receiptCreated = ReferendumReceiptCreated.Parser.ParseFrom(transactionResult.TransactionResult.Logs
            .FirstOrDefault(l => l.Name == nameof(ReferendumReceiptCreated))
            ?.NonIndexed);
        ValidateReferendumReceiptCreated(receiptCreated, Address.FromPublicKey(reviewer.PublicKey), proposalId, utcNow,
            nameof(referendumContractStub.Approve));
    }

    private async Task RejectAsync(ECKeyPair reviewer, Hash proposalId)
    {
        var referendumContractStub = GetReferendumContractTester(reviewer);
        var utcNow = TimestampHelper.GetUtcNow();
        BlockTimeProvider.SetBlockTime(utcNow);
        var transactionResult = await referendumContractStub.Reject.SendAsync(proposalId);
        transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var receiptCreated = ReferendumReceiptCreated.Parser.ParseFrom(transactionResult.TransactionResult.Logs
            .FirstOrDefault(l => l.Name == nameof(ReferendumReceiptCreated))
            ?.NonIndexed);
        ValidateReferendumReceiptCreated(receiptCreated, Address.FromPublicKey(reviewer.PublicKey), proposalId, utcNow,
            nameof(referendumContractStub.Reject));
    }

    private async Task AbstainAsync(ECKeyPair reviewer, Hash proposalId)
    {
        var referendumContractStub = GetReferendumContractTester(reviewer);
        var utcNow = TimestampHelper.GetUtcNow();
        BlockTimeProvider.SetBlockTime(utcNow);
        var transactionResult = await referendumContractStub.Abstain.SendAsync(proposalId);
        transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var receiptCreated = ReferendumReceiptCreated.Parser.ParseFrom(transactionResult.TransactionResult.Logs
            .FirstOrDefault(l => l.Name == nameof(ReferendumReceiptCreated))
            ?.NonIndexed);
        ValidateReferendumReceiptCreated(receiptCreated, Address.FromPublicKey(reviewer.PublicKey), proposalId, utcNow,
            nameof(referendumContractStub.Abstain));
    }

    private void ValidateReferendumReceiptCreated(ReferendumReceiptCreated referendumReceiptCreated, Address sender,
        Hash proposalId,
        Timestamp blockTime, string receiptType)
    {
        referendumReceiptCreated.Address.ShouldBe(sender);
        referendumReceiptCreated.ProposalId.ShouldBe(proposalId);
        referendumReceiptCreated.Time.ShouldBe(blockTime);
        referendumReceiptCreated.ReceiptType.ShouldBe(receiptType);
    }

    private async Task ApproveAllowanceAsync(ECKeyPair keyPair, long amount, Hash proposalId, string symbol = "ELF")
    {
        var proposalVirtualAddress = await ReferendumContractStub.GetProposalVirtualAddress.CallAsync(proposalId);
        await GetTokenContractTester(keyPair).Approve.SendAsync(new ApproveInput
        {
            Amount = amount,
            Spender = proposalVirtualAddress,
            Symbol = symbol
        });
    }

    private async Task<Hash> CreateFeeProposalAsync(Address contractAddress, Address organizationAddress,
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

    private async Task ApproveWithMinersAsync(Hash proposalId)
    {
        foreach (var bp in InitialCoreDataCenterKeyPairs)
        {
            var tester = GetParliamentContractTester(bp);
            var approveResult = await tester.Approve.SendAsync(proposalId);
            approveResult.TransactionResult.Error.ShouldBeNullOrEmpty();
        }
    }
    private async Task ApproveAndTransferCreateTokenFee(ECKeyPair proposalKeyPair, long minimalApproveThreshold,
        Address organizationAddress)
    {
        var approveInput = new ApproveInput
        {
            Spender = TokenContractAddress,
            Symbol = "ELF",
            Amount = 10000_00000000
        };
        var proposalId =
            await CreateReferendumProposalAsync(proposalKeyPair, approveInput, "Approve", organizationAddress,
                TokenContractAddress);
        await ApproveAllowanceAsync(Accounts[3].KeyPair, minimalApproveThreshold, proposalId);
        await ApproveAsync(Accounts[3].KeyPair, proposalId);
        var getProposal = await ReferendumContractStub.GetProposal.CallAsync(proposalId);
        getProposal.ToBeReleased.ShouldBeTrue();

        var result = await ReferendumContractStub.Release.SendAsync(proposalId);
        result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

        await TokenContractStub.Transfer.SendAsync(new TransferInput
        {
            Amount = 10000_00000000,
            Symbol = "ELF",
            To = organizationAddress
        });
    }

    private MethodFees GetValidMethodFees()
    {
        return new MethodFees
        {
            MethodName = nameof(ReferendumContractStub.CreateProposal),
            Fees =
            {
                new MethodFee
                {
                    Symbol = "ELF",
                    BasicFee = 5000_0000L
                }
            }
        };
    }
}