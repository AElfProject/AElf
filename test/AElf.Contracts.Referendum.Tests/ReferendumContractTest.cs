using System.Linq;
using System.Threading.Tasks;
using Acs1;
using Acs3;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Referendum
{
    public class ReferendumContractTest : ReferendumContractTestBase
    {
        public ReferendumContractTest()
        {
            InitializeContracts();
        }

        [Fact]
        public async Task Get_Organization_Test()
        {
            //not exist
            {
                var organization =
                    await ReferendumContractStub.GetOrganization.CallAsync(SampleAddress.AddressList[0]);
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
                    Proposers = {DefaultSender}
                },
                TokenSymbol = "ELF",
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
            getOrganization.OrganizationHash.ShouldBe(HashHelper.ComputeFromMessage(createOrganizationInput));
        }

        [Fact]
        public async Task Get_Proposal_Test()
        {
            //not exist
            {
                var proposal = await ReferendumContractStub.GetProposal.CallAsync(HashHelper.ComputeFromString("Test"));
                proposal.ShouldBe(new ProposalOutput());
            }

            var minimalApproveThreshold = 5000;
            var minimalVoteThreshold = 5000;
            var maximalRejectionThreshold = 10000;
            var maximalAbstentionThreshold = 10000;
            var organizationAddress = await CreateOrganizationAsync(minimalApproveThreshold, minimalVoteThreshold,
                maximalAbstentionThreshold, maximalRejectionThreshold, new[] {DefaultSender});
            var createInput = new CreateInput()
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
                maximalAbstentionThreshold, maximalRejectionThreshold, new[] {DefaultSender});
            var blockTime = BlockTimeProvider.GetBlockTime();

            {
                //"Invalid proposal."
                var createProposalInput = new CreateProposalInput
                {
                    ToAddress = SampleAddress.AddressList[0],
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
                    ToAddress = SampleAddress.AddressList[0],
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
                    ToAddress = SampleAddress.AddressList[0],
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
                    ToAddress = SampleAddress.AddressList[0],
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
                    ToAddress = SampleAddress.AddressList[0],
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
                    ToAddress = SampleAddress.AddressList[0],
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
                    ToAddress = SampleAddress.AddressList[0],
                    Params = ByteString.CopyFromUtf8("Test"),
                    ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                    OrganizationAddress = organizationAddress,
                    ContractMethodName = "Test"
                };
                var transactionResult = await GetReferendumContractTester(SampleECKeyPairs.KeyPairs.Last())
                    .CreateProposal.SendWithExceptionAsync(createProposalInput);
                transactionResult.TransactionResult.Error.ShouldContain("Unauthorized to propose.");
            }
        }

        [Fact]
        public async Task Approve_Proposal_NotFoundProposal_Test()
        {
            var transactionResult =
                await ReferendumContractStub.Approve.SendWithExceptionAsync(HashHelper.ComputeFromString("Test"));
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
                maximalAbstentionThreshold, maximalRejectionThreshold, new[] {DefaultSender});
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
                maximalAbstentionThreshold, maximalRejectionThreshold, new[] {DefaultSender});
            var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
            ReferendumContractStub = GetReferendumContractTester(DefaultSenderKeyPair);
            long amount = 1000;
            await TokenContractStub.Approve.SendAsync(new ApproveInput
            {
                Amount = amount,
                Spender = ReferendumContractAddress,
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
                maximalAbstentionThreshold, maximalRejectionThreshold, new[] {DefaultSender});
            var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
            var keyPair = SampleECKeyPairs.KeyPairs[1];
            var userBalanceBeforeApprove =
                await GetBalanceAsync("ELF", Address.FromPublicKey(keyPair.PublicKey));
            long amount = 1000;
            await GetTokenContractTester(keyPair).Approve.SendAsync(new ApproveInput
            {
                Amount = amount,
                Spender = ReferendumContractAddress,
                Symbol = "ELF"
            });
            var referendumContractStub = GetReferendumContractTester(keyPair);
            await referendumContractStub.Approve.SendAsync(proposalId);
            var userBalance =
                await GetBalanceAsync("ELF", Address.FromPublicKey(SampleECKeyPairs.KeyPairs[1].PublicKey));
            userBalance.ShouldBe(userBalanceBeforeApprove - amount);

            var transactionResult2 = await referendumContractStub.Approve.SendWithExceptionAsync(proposalId);
            transactionResult2.TransactionResult.Error.ShouldContain("Allowance not enough.");

            await GetTokenContractTester(keyPair).Approve.SendAsync(new ApproveInput
            {
                Amount = amount,
                Spender = ReferendumContractAddress,
                Symbol = "ELF"
            });

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
                maximalAbstentionThreshold, maximalRejectionThreshold, new[] {DefaultSender});
            var timeStamp = TimestampHelper.GetUtcNow();
            var proposalId =
                await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress, timeStamp.AddSeconds(5));

            var referendumContractStub = GetReferendumContractTester(SampleECKeyPairs.KeyPairs[1]);
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
                maximalAbstentionThreshold, maximalRejectionThreshold, new[] {DefaultSender}, "ABC");
            var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);

            var keyPair = SampleECKeyPairs.KeyPairs[1];
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
                maximalAbstentionThreshold, maximalRejectionThreshold, new[] {DefaultSender});
            var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
            var amount = 5000;
            var keyPairApprove = SampleECKeyPairs.KeyPairs[3];
            await ApproveAllowanceAsync(keyPairApprove, amount);
            var balanceApprove1 = await GetBalanceAsync("ELF", Address.FromPublicKey(keyPairApprove.PublicKey));
            var keyPairReject = SampleECKeyPairs.KeyPairs[4];
            await ApproveAllowanceAsync(keyPairReject, amount);
            var balanceReject1 = await GetBalanceAsync("ELF", Address.FromPublicKey(keyPairReject.PublicKey));
            var keyPairAbstain = SampleECKeyPairs.KeyPairs[5];
            await ApproveAllowanceAsync(keyPairAbstain, amount);
            var balanceAbstain1 = await GetBalanceAsync("ELF", Address.FromPublicKey(keyPairAbstain.PublicKey));

            await ApproveAsync(keyPairApprove, proposalId);
            await RejectAsync(keyPairReject, proposalId);
            await AbstainAsync(keyPairAbstain, proposalId);

            ReferendumContractStub = GetReferendumContractTester(DefaultSenderKeyPair);
            var result = await ReferendumContractStub.Release.SendAsync(proposalId);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var referendumContractStubApprove = GetReferendumContractTester(keyPairApprove);
            var reclaimResult1 = await referendumContractStubApprove.ReclaimVoteToken.SendAsync(proposalId);
            reclaimResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var referendumContractStubReject = GetReferendumContractTester(keyPairReject);
            var reclaimResult2 = await referendumContractStubReject.ReclaimVoteToken.SendAsync(proposalId);
            reclaimResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var referendumContractStubAbstain = GetReferendumContractTester(keyPairAbstain);
            var reclaimResult3 = await referendumContractStubAbstain.ReclaimVoteToken.SendAsync(proposalId);
            reclaimResult3.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var balanceApprove2 = await GetBalanceAsync("ELF", Address.FromPublicKey(keyPairApprove.PublicKey));
            balanceApprove2.ShouldBe(balanceApprove1);
            var balanceReject2 = await GetBalanceAsync("ELF", Address.FromPublicKey(keyPairReject.PublicKey));
            balanceReject2.ShouldBe(balanceReject1);
            var balanceAbstain2 = await GetBalanceAsync("ELF", Address.FromPublicKey(keyPairAbstain.PublicKey));
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
                maximalAbstentionThreshold, maximalRejectionThreshold, new[] {DefaultSender});
            var timeStamp = TimestampHelper.GetUtcNow();
            BlockTimeProvider.SetBlockTime(timeStamp); // set next block time
            var proposalId =
                await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress, timeStamp.AddSeconds(5));
            var amount = 5000;
            var keyPair = SampleECKeyPairs.KeyPairs[3];
            await ApproveAllowanceAsync(keyPair, amount);
            var balance1 = await GetBalanceAsync("ELF", Address.FromPublicKey(keyPair.PublicKey));

            await ApproveAsync(keyPair, proposalId);
            BlockTimeProvider.SetBlockTime(timeStamp.AddSeconds(10)); // set next block time
            var referendumContractStubApprove = GetReferendumContractTester(keyPair);
            var reclaimResult = await referendumContractStubApprove.ReclaimVoteToken.SendAsync(proposalId);
            reclaimResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var balance2 = await GetBalanceAsync("ELF", Address.FromPublicKey(keyPair.PublicKey));
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
                maximalAbstentionThreshold, maximalRejectionThreshold, new[] {DefaultSender});
            var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);

            var keyPair = SampleECKeyPairs.KeyPairs[1];
            var amount = 2000;
            await ApproveAllowanceAsync(keyPair, amount);
            await ApproveAsync(keyPair, proposalId);

            var reclaimResult = await GetReferendumContractTester(keyPair).ReclaimVoteToken
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
                maximalAbstentionThreshold, maximalRejectionThreshold, new[] {DefaultSender});
            var timeStamp = TimestampHelper.GetUtcNow();
            var proposalId =
                await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);

            var referendumContractStub = GetReferendumContractTester(SampleECKeyPairs.KeyPairs[1]);
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
                maximalAbstentionThreshold, maximalRejectionThreshold, new[] {DefaultSender});
            //Abstain probability >= maximalAbstentionThreshold
            {
                var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
                var keyPair1 = SampleECKeyPairs.KeyPairs[1];
                var amount = 1001;
                await ApproveAllowanceAsync(keyPair1, amount);
                await ApproveAsync(keyPair1, proposalId);
                var result = await ReferendumContractStub.GetProposal.CallAsync(proposalId);
                result.ToBeReleased.ShouldBeTrue();
                var keyPair2 = SampleECKeyPairs.KeyPairs[2];
                await ApproveAllowanceAsync(keyPair2, amount);
                await AbstainAsync(keyPair2, proposalId);
                result = await ReferendumContractStub.GetProposal.CallAsync(proposalId);
                result.ToBeReleased.ShouldBeFalse();
            }
            //Rejection probability > maximalRejectionThreshold
            {
                var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
                var keyPair1 = SampleECKeyPairs.KeyPairs[1];
                var amount = 1001;
                await ApproveAllowanceAsync(keyPair1, amount);
                await ApproveAsync(keyPair1, proposalId);
                var result = await ReferendumContractStub.GetProposal.CallAsync(proposalId);
                result.ToBeReleased.ShouldBeTrue();
                var keyPair2 = SampleECKeyPairs.KeyPairs[2];
                await ApproveAllowanceAsync(keyPair2, amount);
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
                maximalAbstentionThreshold, maximalRejectionThreshold, new[] {DefaultSender});
            var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
            var keyPair = SampleECKeyPairs.KeyPairs[3];
            var amount = 2000;
            await ApproveAllowanceAsync(keyPair, amount);
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
            var proposalId = HashHelper.ComputeFromString("test");
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
                maximalAbstentionThreshold, maximalRejectionThreshold, new[] {DefaultSender});
            var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
            var keyPair = SampleECKeyPairs.KeyPairs[3];
            var amount = 5000;
            await ApproveAllowanceAsync(keyPair, amount);
            await ApproveAsync(keyPair, proposalId);

            var referendumContractStub = GetReferendumContractTester(SampleECKeyPairs.KeyPairs[3]);
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
                maximalAbstentionThreshold, maximalRejectionThreshold, new[] {DefaultSender});
            var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
            await GetTokenContractTester(SampleECKeyPairs.KeyPairs[3]).Approve.SendAsync(new ApproveInput
            {
                Amount = minimalApproveThreshold,
                Spender = ReferendumContractAddress,
                Symbol = "ELF"
            });

            await ApproveAsync(SampleECKeyPairs.KeyPairs[3], proposalId);

            ReferendumContractStub = GetReferendumContractTester(DefaultSenderKeyPair);
            var result = await ReferendumContractStub.Release.SendAsync(proposalId);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            // Check inline transaction result
            var newToken = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput {Symbol = "NEW"});
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
                maximalAbstentionThreshold, maximalRejectionThreshold, new[] {DefaultSender});
            var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);

            {
                var amount = 5000;
                var keyPair = SampleECKeyPairs.KeyPairs[3];
                await ApproveAllowanceAsync(keyPair, amount);
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
                var keyPair = SampleECKeyPairs.KeyPairs[3];
                await ApproveAllowanceAsync(keyPair, amount);

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
                maximalAbstentionThreshold, maximalRejectionThreshold, new[] {DefaultSender});
            var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
            var keyPair = SampleECKeyPairs.KeyPairs[3];
            await ApproveAllowanceAsync(keyPair, minimalApproveThreshold);
            await ApproveAsync(SampleECKeyPairs.KeyPairs[3], proposalId);
            var proposal = await ReferendumContractStub.GetProposal.CallAsync(proposalId);
            proposal.ToBeReleased.ShouldBeTrue();

            {
                var proposalReleaseThresholdInput = new ProposalReleaseThreshold
                {
                    MinimalVoteThreshold = 20000
                };

                var referendumContractStub = GetReferendumContractTester(DefaultSenderKeyPair);
                var changeProposalId = await CreateReferendumProposalAsync(DefaultSenderKeyPair,
                    proposalReleaseThresholdInput,
                    nameof(referendumContractStub.ChangeOrganizationThreshold), organizationAddress);
                await ApproveAllowanceAsync(keyPair, minimalApproveThreshold);
                await ApproveAsync(SampleECKeyPairs.KeyPairs[3], changeProposalId);
                referendumContractStub = GetReferendumContractTester(DefaultSenderKeyPair);
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
                    nameof(ReferendumContractStub.ChangeOrganizationThreshold), organizationAddress);
                await ApproveAllowanceAsync(keyPair, minimalApproveThreshold);
                await ApproveAsync(SampleECKeyPairs.KeyPairs[3], changeProposalId);
                var referendumContractStub = GetReferendumContractTester(DefaultSenderKeyPair);
                var result = await referendumContractStub.Release.SendAsync(changeProposalId);
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                proposal = await referendumContractStub.GetProposal.CallAsync(proposalId);
                proposal.ToBeReleased.ShouldBeFalse();
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
                maximalAbstentionThreshold, maximalRejectionThreshold, new[] {DefaultSender});

            var whiteAddress = Address.FromPublicKey(SampleECKeyPairs.KeyPairs[3].PublicKey);
            var proposerWhiteList = new ProposerWhiteList
            {
                Proposers = {whiteAddress}
            };

            ReferendumContractStub = GetReferendumContractTester(DefaultSenderKeyPair);
            var changeProposalId = await CreateReferendumProposalAsync(DefaultSenderKeyPair, proposerWhiteList,
                nameof(ReferendumContractStub.ChangeOrganizationProposerWhiteList), organizationAddress);
            await GetTokenContractTester(SampleECKeyPairs.KeyPairs[3]).Approve.SendAsync(new ApproveInput
            {
                Amount = minimalApproveThreshold,
                Spender = ReferendumContractAddress,
                Symbol = "ELF"
            });
            await ApproveAsync(SampleECKeyPairs.KeyPairs[3], changeProposalId);
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

        private async Task<Hash> CreateProposalAsync(ECKeyPair proposalKeyPair, Address organizationAddress,
            Timestamp timestamp = null)
        {
            var createInput = new CreateInput()
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
            string method, Address organizationAddress)
        {
            var referendumContractStub = GetReferendumContractTester(proposalKeyPair);
            var createProposalInput = new CreateProposalInput
            {
                ContractMethodName = method,
                ToAddress = ReferendumContractAddress,
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

        private async Task<Address> CreateOrganizationAsync(long minimalApproveThreshold, long minimalVoteThreshold,
            long maximalAbstentionThreshold, long maximalRejectionThreshold, Address[] proposerWhiteList,
            string symbol = "ELF")
        {
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
                    Proposers = {proposerWhiteList}
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
                nameof(referendumContractStub.Abstain));}

        private void ValidateReferendumReceiptCreated(ReferendumReceiptCreated referendumReceiptCreated, Address sender, Hash proposalId,
            Timestamp blockTime, string receiptType)
        {
            referendumReceiptCreated.Address.ShouldBe(sender);
            referendumReceiptCreated.ProposalId.ShouldBe(proposalId);
            referendumReceiptCreated.Time.ShouldBe(blockTime);
            referendumReceiptCreated.ReceiptType.ShouldBe(receiptType);
        }
        
        private async Task ApproveAllowanceAsync(ECKeyPair keyPair, long amount, string symbol = "ELF")
        {
            await GetTokenContractTester(keyPair).Approve.SendAsync(new ApproveInput
            {
                Amount = amount,
                Spender = ReferendumContractAddress,
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
    }
}