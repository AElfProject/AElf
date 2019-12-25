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
            getOrganization.OrganizationHash.ShouldBe(Hash.FromTwoHashes(
                Hash.FromMessage(ReferendumContractAddress), Hash.FromMessage(createOrganizationInput)));
        }

        [Fact]
        public async Task Get_Proposal_Test()
        {
            //not exist
            {
                var proposal = await ReferendumContractStub.GetProposal.CallAsync(Hash.FromString("Test"));
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
                await ReferendumContractStub.Approve.SendWithExceptionAsync(Hash.FromString("Test"));
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
            await TokenContractStub.Approve.SendAsync(new MultiToken.ApproveInput
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
            long amount = 1000;
            await GetTokenContractTester(keyPair).Approve.SendAsync(new MultiToken.ApproveInput
            {
                Amount = amount,
                Spender = ReferendumContractAddress,
                Symbol = "ELF"
            });
            var referendumContractStub = GetReferendumContractTester(keyPair);
            await referendumContractStub.Approve.SendAsync(proposalId);
            var userBalance =
                await GetBalanceAsync("ELF", Address.FromPublicKey(SampleECKeyPairs.KeyPairs[1].PublicKey));
            userBalance.ShouldBe(10000 - 1000);

            var transactionResult2 = await referendumContractStub.Approve.SendWithExceptionAsync(proposalId);
            transactionResult2.TransactionResult.Error.ShouldContain("Allowance not enough.");
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
            await GetTokenContractTester(keyPair).Approve.SendAsync(new MultiToken.ApproveInput
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
            var keyPair = SampleECKeyPairs.KeyPairs[3];
            await ApproveAllowanceAsync(keyPair, amount);
            var balance1 = await GetBalanceAsync("ELF", Address.FromPublicKey(keyPair.PublicKey));
        
            await ApproveAsync(keyPair, proposalId);
        
            ReferendumContractStub = GetReferendumContractTester(DefaultSenderKeyPair);
            var result = await ReferendumContractStub.Release.SendAsync(proposalId);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        
            var referendumContractStubApprove = GetReferendumContractTester(keyPair);
            var reclaimResult = await referendumContractStubApprove.ReclaimVoteToken.SendAsync(proposalId);
            reclaimResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        
            var balance2 = await GetBalanceAsync("ELF", Address.FromPublicKey(keyPair.PublicKey));
            balance2.ShouldBe(balance1);
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
            var proposalId = Hash.FromString("test");
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
            await GetTokenContractTester(SampleECKeyPairs.KeyPairs[3]).Approve.SendAsync(new MultiToken.ApproveInput
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
                var proposalReleased = ProposalReleased.Parser.ParseFrom(result.TransactionResult.Logs[0].NonIndexed)
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
            var transactionResult = await referendumContractStub.Approve.SendAsync(proposalId);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        private async Task ApproveAllowanceAsync(ECKeyPair keyPair, long amount, string symbol = "ELF")
        {
            await GetTokenContractTester(keyPair).Approve.SendAsync(new MultiToken.ApproveInput
            {
                Amount = amount,
                Spender = ReferendumContractAddress,
                Symbol = symbol
            });
        }
    }
}