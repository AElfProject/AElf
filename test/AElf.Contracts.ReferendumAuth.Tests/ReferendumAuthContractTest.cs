using System.Threading;
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

namespace AElf.Contracts.ReferendumAuth
{
    public class ReferendumAuthContractTest : ReferendumAuthContractTestBase
    {
        public ReferendumAuthContractTest()
        {
            InitializeContracts();
        }

        [Fact]
        public async Task Get_Organization_Test()
        {
            //not exist
            {
                var organization =
                    await ReferendumAuthContractStub.GetOrganization.CallAsync(SampleAddress.AddressList[0]);
                organization.ShouldBe(new Organization());
            }

            var createOrganizationInput = new CreateOrganizationInput
            {
                ReleaseThreshold = 5000,
                TokenSymbol = "ELF",
            };
            var organizationAddress = await CreateOrganizationAsync();
            var getOrganization = await ReferendumAuthContractStub.GetOrganization.CallAsync(organizationAddress);

            getOrganization.OrganizationAddress.ShouldBe(organizationAddress);
            getOrganization.ReleaseThreshold.ShouldBe(5000);
            getOrganization.OrganizationHash.ShouldBe(Hash.FromTwoHashes(
                Hash.FromMessage(ReferendumAuthContractAddress), Hash.FromMessage(createOrganizationInput)));
        }

        [Fact]
        public async Task Get_Proposal_Test()
        {
            //not exist
            {
                var proposal = await ReferendumAuthContractStub.GetProposal.CallAsync(Hash.FromString("Test"));
                proposal.ShouldBe(new ProposalOutput());
            }

            var organizationAddress = await CreateOrganizationAsync();
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
            var getProposal = await ReferendumAuthContractStub.GetProposal.SendAsync(proposalId);

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
            var organizationAddress = await CreateOrganizationAsync();
            var blockTime = BlockTimeProvider.GetBlockTime();
            var createProposalInput = new CreateProposalInput
            {
                ToAddress = SampleAddress.AddressList[0],
                Params = ByteString.CopyFromUtf8("Test"),
                ExpiredTime = blockTime.AddDays(1),
                OrganizationAddress = organizationAddress
            };
            {
                //"Invalid proposal."
                var transactionResult =
                    await ReferendumAuthContractStub.CreateProposal.SendWithExceptionAsync(createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
            }
            {
                createProposalInput.ContractMethodName = "Test";
                createProposalInput.ToAddress = null;

                var transactionResult =
                    await ReferendumAuthContractStub.CreateProposal.SendWithExceptionAsync(createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
            }
            {
                createProposalInput.ExpiredTime = null;
                createProposalInput.ToAddress = SampleAddress.AddressList[0];

                var transactionResult =
                    await ReferendumAuthContractStub.CreateProposal.SendWithExceptionAsync(createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
            }
            {
                //"Expired proposal."
                createProposalInput.ExpiredTime = blockTime.AddMilliseconds(5);
                Thread.Sleep(10);

                var transactionResult =
                    await ReferendumAuthContractStub.CreateProposal.SendWithExceptionAsync(createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            }
            {
                //"No registered organization."
                createProposalInput.ExpiredTime = BlockTimeProvider.GetBlockTime().AddDays(1);
                createProposalInput.OrganizationAddress = SampleAddress.AddressList[1];

                var transactionResult =
                    await ReferendumAuthContractStub.CreateProposal.SendWithExceptionAsync(createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("No registered organization.").ShouldBeTrue();
            }
            {
                //"Proposal with same input."
                createProposalInput.OrganizationAddress = organizationAddress;
                var transactionResult1 = await ReferendumAuthContractStub.CreateProposal.SendAsync(createProposalInput);
                transactionResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var transactionResult2 = await ReferendumAuthContractStub.CreateProposal.SendAsync(createProposalInput);
                transactionResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
        }

        [Fact]
        public async Task Approve_Proposal_NotFoundProposal_Test()
        {
            var transactionResult =
                await ReferendumAuthContractStub.Approve.SendWithExceptionAsync(Hash.FromString("Test"));
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("Invalid proposal id").ShouldBeTrue();
        }

        [Fact]
        public async Task Approve_WithoutAllowance()
        {
            var organizationAddress = await CreateOrganizationAsync();
            var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
            ReferendumAuthContractStub = GetReferendumAuthContractTester(DefaultSenderKeyPair);
            var approveTx = await ReferendumAuthContractStub.Approve.SendWithExceptionAsync(proposalId);
            approveTx.TransactionResult.Error.ShouldContain("Invalid approve.");
        }

        [Fact]
        public async Task Approve_Success()
        {
            var organizationAddress = await CreateOrganizationAsync();
            var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
            ReferendumAuthContractStub = GetReferendumAuthContractTester(DefaultSenderKeyPair);
            long amount = 1000;
            await TokenContractStub.Approve.SendAsync(new MultiToken.ApproveInput
            {
                Amount = amount,
                Spender = ReferendumAuthContractAddress,
                Symbol = "ELF"
            });
            var balance1 = await GetBalanceAsync("ELF", DefaultSender);
            var transactionResult1 = await ReferendumAuthContractStub.Approve.SendAsync(proposalId);
            transactionResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var balance2 = await GetBalanceAsync("ELF", DefaultSender);
            balance2.ShouldBe(balance1 - amount);
        }

        [Fact]
        public async Task Approve_Proposal_MultiTimes_Test()
        {
            var organizationAddress = await CreateOrganizationAsync();
            var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
            var keyPair = SampleECKeyPairs.KeyPairs[1];
            long amount = 1000;
            await GetTokenContractTester(keyPair).Approve.SendAsync(new MultiToken.ApproveInput
            {
                Amount = amount,
                Spender = ReferendumAuthContractAddress,
                Symbol = "ELF"
            });
            ReferendumAuthContractStub = GetReferendumAuthContractTester(keyPair);
            await ReferendumAuthContractStub.Approve.SendAsync(proposalId);
            var userBalance =
                await GetBalanceAsync("ELF", Address.FromPublicKey(SampleECKeyPairs.KeyPairs[1].PublicKey));
            userBalance.ShouldBe(10000 - 1000);

            Thread.Sleep(100);
            var transactionResult2 = await ReferendumAuthContractStub.Approve.SendWithExceptionAsync(proposalId);
            transactionResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult2.TransactionResult.Error.Contains("Cannot approve more than once.").ShouldBeTrue();
        }

        [Fact]
        public async Task Approve_Proposal_ExpiredTime_Test()
        {
            var organizationAddress = await CreateOrganizationAsync();
            var timeStamp = TimestampHelper.GetUtcNow();
            var proposalId =
                await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress, timeStamp.AddSeconds(5));

            var referendumAuthContractStub = GetReferendumAuthContractTester(SampleECKeyPairs.KeyPairs[1]);
            BlockTimeProvider.SetBlockTime(timeStamp.AddSeconds(10));

            var error = await referendumAuthContractStub.Approve.CallWithExceptionAsync(proposalId);
            error.Value.ShouldContain("Invalid proposal.");
        }

        [Fact]
        public async Task Approve_WrongAllowance()
        {
            var organizationAddress = await CreateOrganizationAsync("ABC");
            var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);

            var keyPair = SampleECKeyPairs.KeyPairs[1];
            long amount = 1000;
            await GetTokenContractTester(keyPair).Approve.SendAsync(new MultiToken.ApproveInput
            {
                Amount = amount,
                Spender = ReferendumAuthContractAddress,
                Symbol = "ELF"
            });
            ReferendumAuthContractStub = GetReferendumAuthContractTester(keyPair);
            var approveTransaction = await ReferendumAuthContractStub.Approve.SendWithExceptionAsync(proposalId);
            approveTransaction.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            approveTransaction.TransactionResult.Error.Contains("Invalid approve.").ShouldBeTrue();
        }

        [Fact]
        public async Task ReclaimVoteToken_AfterRelease()
        {
            var organizationAddress = await CreateOrganizationAsync();
            var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
            var amount = 5000;
            var keyPair = SampleECKeyPairs.KeyPairs[3];
            await ApproveAllowanceAsync(keyPair, amount);
            var balance1 = await GetBalanceAsync("ELF", Address.FromPublicKey(keyPair.PublicKey));

            await ApproveAsync(keyPair, proposalId);

            ReferendumAuthContractStub = GetReferendumAuthContractTester(DefaultSenderKeyPair);
            var result = await ReferendumAuthContractStub.Release.SendAsync(proposalId);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var referendumAuthContractStubApprove = GetReferendumAuthContractTester(keyPair);
            var reclaimResult = await referendumAuthContractStubApprove.ReclaimVoteToken.SendAsync(proposalId);
            reclaimResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var balance2 = await GetBalanceAsync("ELF", Address.FromPublicKey(keyPair.PublicKey));
            balance2.ShouldBe(balance1);
        }

        [Fact]
        public async Task ReclaimVoteToken_AfterExpired()
        {
            var organizationAddress = await CreateOrganizationAsync();
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
            var referendumAuthContractStubApprove = GetReferendumAuthContractTester(keyPair);
            var reclaimResult = await referendumAuthContractStubApprove.ReclaimVoteToken.SendAsync(proposalId);
            reclaimResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var balance2 = await GetBalanceAsync("ELF", Address.FromPublicKey(keyPair.PublicKey));
            balance2.ShouldBe(balance1);
        }

        [Fact]
        public async Task ReclaimVoteToken_WithoutRelease()
        {
            var organizationAddress = await CreateOrganizationAsync();
            var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);

            var keyPair = SampleECKeyPairs.KeyPairs[1];
            var amount = 2000;
            await ApproveAllowanceAsync(keyPair, amount);
            await ApproveAsync(keyPair, proposalId);

            var reclaimResult = await GetReferendumAuthContractTester(keyPair).ReclaimVoteToken
                .SendWithExceptionAsync(proposalId);
            reclaimResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            reclaimResult.TransactionResult.Error.Contains("Unable to reclaim at this time.").ShouldBeTrue();
        }

        [Fact]
        public async Task Reclaim_VoteTokenWithoutVote_Test()
        {
            var organizationAddress = await CreateOrganizationAsync();
            var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);

            ReferendumAuthContractStub = GetReferendumAuthContractTester(SampleECKeyPairs.KeyPairs[1]);
            var reclaimResult = await ReferendumAuthContractStub.ReclaimVoteToken.SendWithExceptionAsync(proposalId);
            reclaimResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            reclaimResult.TransactionResult.Error.Contains("Nothing to reclaim.").ShouldBeTrue();
        }

        [Fact]
        public async Task Release_NotEnoughWeight_Test()
        {
            var organizationAddress = await CreateOrganizationAsync();
            var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
            var keyPair = SampleECKeyPairs.KeyPairs[3];
            var amount = 2000;
            await ApproveAllowanceAsync(keyPair, amount);
            await ApproveAsync(keyPair, proposalId);

            ReferendumAuthContractStub = GetReferendumAuthContractTester(DefaultSenderKeyPair);
            var result = await ReferendumAuthContractStub.Release.SendWithExceptionAsync(proposalId);
            //Reviewer Shares < ReleaseThreshold, release failed
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.Contains("Not approved.").ShouldBeTrue();
        }

        [Fact]
        public async Task Release_NotFound_Test()
        {
            var proposalId = Hash.FromString("test");
            var result = await ReferendumAuthContractStub.Release.SendWithExceptionAsync(proposalId);
            //Proposal not found
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.Contains("Proposal not found.").ShouldBeTrue();
        }

        [Fact]
        public async Task Release_WrongSender_Test()
        {
            var organizationAddress = await CreateOrganizationAsync();
            var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
            var keyPair = SampleECKeyPairs.KeyPairs[3];
            var amount = 5000;
            await ApproveAllowanceAsync(keyPair, amount);
            await ApproveAsync(keyPair, proposalId);

            ReferendumAuthContractStub = GetReferendumAuthContractTester(SampleECKeyPairs.KeyPairs[3]);
            var result = await ReferendumAuthContractStub.Release.SendWithExceptionAsync(proposalId);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.Contains("Unable to release this proposal.").ShouldBeTrue();
        }

        [Fact]
        public async Task Release_Proposal_Test()
        {
            long amount = 5000;
            var organizationAddress = await CreateOrganizationAsync("ELF", amount);
            var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
            await GetTokenContractTester(SampleECKeyPairs.KeyPairs[3]).Approve.SendAsync(new MultiToken.ApproveInput
            {
                Amount = amount,
                Spender = ReferendumAuthContractAddress,
                Symbol = "ELF"
            });

            await ApproveAsync(SampleECKeyPairs.KeyPairs[3], proposalId);

            ReferendumAuthContractStub = GetReferendumAuthContractTester(DefaultSenderKeyPair);
            var result = await ReferendumAuthContractStub.Release.SendAsync(proposalId);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            // Check inline transaction result
            var newToken = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput {Symbol = "NEW"});
            newToken.Issuer.ShouldBe(organizationAddress);
        }

        [Fact]
        public async Task Release_Proposal_AlreadyReleased_Test()
        {
            var organizationAddress = await CreateOrganizationAsync();
            var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);

            {
                var amount = 5000;
                var keyPair = SampleECKeyPairs.KeyPairs[3];
                await ApproveAllowanceAsync(keyPair, amount);
                await ApproveAsync(keyPair, proposalId);

                var referendumAuthContractStub = GetReferendumAuthContractTester(DefaultSenderKeyPair);
                var result = await referendumAuthContractStub.Release.SendAsync(proposalId);
                result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                var proposalReleased = ProposalReleased.Parser.ParseFrom(result.TransactionResult.Logs[0].NonIndexed)
                    .ProposalId;
                proposalReleased.ShouldBe(proposalId);

                //After release,the proposal will be deleted
                var getProposal = await ReferendumAuthContractStub.GetProposal.CallAsync(proposalId);
                getProposal.ShouldBe(new ProposalOutput());
            }
            {
                var amount = 5000;
                var keyPair = SampleECKeyPairs.KeyPairs[3];
                await ApproveAllowanceAsync(keyPair, amount);

                //approve the same proposal again
                var referendumAuthContractStub = GetReferendumAuthContractTester(keyPair);
                var transactionResult = await referendumAuthContractStub.Approve.SendWithExceptionAsync(proposalId);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.ShouldContain("Invalid proposal");

                //release the same proposal again
                var anotherReferendumAuthContractStub = GetReferendumAuthContractTester(DefaultSenderKeyPair);
                var transactionResult2 =
                    await anotherReferendumAuthContractStub.Release.SendWithExceptionAsync(proposalId);
                transactionResult2.TransactionResult.Error.ShouldContain("Proposal not found.");
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
            ReferendumAuthContractStub = GetReferendumAuthContractTester(proposalKeyPair);
            var proposal = await ReferendumAuthContractStub.CreateProposal.SendAsync(createProposalInput);
            proposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            return proposal.Output;
        }

        private async Task<Address> CreateOrganizationAsync(string symbol = "ELF", long releaseThreshold = 5000)
        {
            var createOrganizationInput = new CreateOrganizationInput
            {
                ReleaseThreshold = releaseThreshold,
                TokenSymbol = symbol,
            };
            var transactionResult =
                await ReferendumAuthContractStub.CreateOrganization.SendAsync(createOrganizationInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            return transactionResult.Output;
        }

        private async Task ApproveAsync(ECKeyPair reviewer, Hash proposalId)
        {
            var referendumAuthContractStub = GetReferendumAuthContractTester(reviewer);
            var transactionResult = await referendumAuthContractStub.Approve.SendAsync(proposalId);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        private async Task ApproveAllowanceAsync(ECKeyPair keyPair, long amount, string symbol = "ELF")
        {
            await GetTokenContractTester(keyPair).Approve.SendAsync(new MultiToken.ApproveInput
            {
                Amount = amount,
                Spender = ReferendumAuthContractAddress,
                Symbol = symbol
            });
        }
    }
}