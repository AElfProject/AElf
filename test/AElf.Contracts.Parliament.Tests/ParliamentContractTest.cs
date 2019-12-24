using System.Linq;
using System.Threading.Tasks;
using Acs3;
using AElf.Contracts.MultiToken;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using SampleAddress = AElf.Contracts.TestKit.SampleAddress;

namespace AElf.Contracts.Parliament
{
    public class ParliamentContractTest : ParliamentContractTestBase
    {
        public ParliamentContractTest()
        {
            InitializeContracts();
        }

        [Fact]
        public async Task Get_DefaultOrganizationAddressFailed_Test()
        {
            var transactionResult =
                await ParliamentContractStub.GetDefaultOrganizationAddress.SendWithExceptionAsync(new Empty());
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("Not initialized.").ShouldBeTrue();
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
            var transactionResult =
                await ParliamentContractStub.CreateOrganization.SendAsync(createOrganizationInput);
            var organizationAddress = transactionResult.Output;
            var getOrganization = await ParliamentContractStub.GetOrganization.CallAsync(organizationAddress);

            getOrganization.OrganizationAddress.ShouldBe(organizationAddress);
            getOrganization.ProposalReleaseThreshold.MinimalApprovalThreshold.ShouldBe(minimalApprovalThreshold);
            getOrganization.ProposalReleaseThreshold.MinimalVoteThreshold.ShouldBe(minimalVoteThreshold);
            getOrganization.ProposalReleaseThreshold.MaximalAbstentionThreshold.ShouldBe(maximalAbstentionThreshold);
            getOrganization.ProposalReleaseThreshold.MaximalRejectionThreshold.ShouldBe(maximalRejectionThreshold);
            getOrganization.OrganizationHash.ShouldBe(Hash.FromTwoHashes(
                Hash.FromMessage(ParliamentContractAddress), Hash.FromMessage(createOrganizationInput)));
        }

        [Fact]
        public async Task Get_OrganizationFailed_Test()
        {
            var organization =
                await ParliamentContractStub.GetOrganization.CallAsync(SampleAddress.AddressList[0]);
            organization.ShouldBe(new Organization());
        }

        [Fact]
        public async Task Get_Proposal_Test()
        {
            var minimalApprovalThreshold = 6667;
            var maximalAbstentionThreshold = 2000;
            var maximalRejectionThreshold = 3000;
            var minimalVoteThreshold = 8000;
            var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
                maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);
            var transferInput = new TransferInput()
            {
                Symbol = "ELF",
                Amount = 100,
                To = Tester,
                Memo = "Transfer"
            };
            var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
            var getProposal = await ParliamentContractStub.GetProposal.SendAsync(proposalId);

            getProposal.Output.Proposer.ShouldBe(DefaultSender);
            getProposal.Output.ContractMethodName.ShouldBe(nameof(TokenContractStub.Transfer));
            getProposal.Output.ProposalId.ShouldBe(proposalId);
            getProposal.Output.OrganizationAddress.ShouldBe(organizationAddress);
            getProposal.Output.ToAddress.ShouldBe(TokenContractAddress);
            getProposal.Output.Params.ShouldBe(transferInput.ToByteString());
        }

        [Fact]
        public async Task Get_ProposalFailed_Test()
        {
            var proposalOutput = await ParliamentContractStub.GetProposal.CallAsync(Hash.FromString("Test"));
            proposalOutput.ShouldBe(new ProposalOutput());
        }

        [Fact]
        public async Task Create_OrganizationFailed_Test()
        {
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

            {
                var transactionResult =
                    await ParliamentContractStub.CreateOrganization.SendAsync(createOrganizationInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            {
                createOrganizationInput.ProposalReleaseThreshold = proposalReleaseThreshold;
                createOrganizationInput.ProposalReleaseThreshold.MinimalApprovalThreshold = 10000;
                createOrganizationInput.ProposalReleaseThreshold.MinimalVoteThreshold = 10000;
                createOrganizationInput.ProposalReleaseThreshold.MaximalAbstentionThreshold = 0;
                createOrganizationInput.ProposalReleaseThreshold.MaximalRejectionThreshold = 0;
                var transactionResult =
                    await ParliamentContractStub.CreateOrganization.SendAsync(createOrganizationInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }

            {
                createOrganizationInput.ProposalReleaseThreshold = proposalReleaseThreshold;
                createOrganizationInput.ProposalReleaseThreshold.MinimalApprovalThreshold = 0;
                var transactionResult =
                    await ParliamentContractStub.CreateOrganization.SendWithExceptionAsync(createOrganizationInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
            }

            {
                createOrganizationInput.ProposalReleaseThreshold = proposalReleaseThreshold;
                createOrganizationInput.ProposalReleaseThreshold.MinimalApprovalThreshold = -1;
                var transactionResult =
                    await ParliamentContractStub.CreateOrganization.SendWithExceptionAsync(createOrganizationInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
            }

            {
                createOrganizationInput.ProposalReleaseThreshold = proposalReleaseThreshold;
                createOrganizationInput.ProposalReleaseThreshold.MaximalAbstentionThreshold = -1;
                var transactionResult =
                    await ParliamentContractStub.CreateOrganization.SendWithExceptionAsync(createOrganizationInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
            }

            {
                createOrganizationInput.ProposalReleaseThreshold = proposalReleaseThreshold;
                createOrganizationInput.ProposalReleaseThreshold.MaximalAbstentionThreshold = 3334;
                var transactionResult =
                    await ParliamentContractStub.CreateOrganization.SendWithExceptionAsync(createOrganizationInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
            }

            {
                createOrganizationInput.ProposalReleaseThreshold = proposalReleaseThreshold;
                createOrganizationInput.ProposalReleaseThreshold.MaximalRejectionThreshold = 3334;
                var transactionResult =
                    await ParliamentContractStub.CreateOrganization.SendWithExceptionAsync(createOrganizationInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
            }

            {
                createOrganizationInput.ProposalReleaseThreshold = proposalReleaseThreshold;
                createOrganizationInput.ProposalReleaseThreshold.MinimalApprovalThreshold = 10001;
                var transactionResult =
                    await ParliamentContractStub.CreateOrganization.SendWithExceptionAsync(createOrganizationInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
            }
        }

        [Fact]
        public async Task Create_ProposalFailed_Test()
        {
            var minimalApprovalThreshold = 6667;
            var maximalAbstentionThreshold = 2000;
            var maximalRejectionThreshold = 3000;
            var minimalVoteThreshold = 8000;
            var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
                maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);
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
                createProposalInput.ToAddress = SampleAddress.AddressList[0];

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
                createProposalInput.OrganizationAddress = SampleAddress.AddressList[1];

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
        }

        [Fact]
        public async Task Approve_Proposal_NotFoundProposal_Test()
        {
            var transactionResult =
                await ParliamentContractStub.Approve.SendWithExceptionAsync(Hash.FromString("Test"));
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        }

        [Fact]
        public async Task Approve_Proposal_NotAuthorizedApproval_Test()
        {
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
            transactionResult.TransactionResult.Error.Contains("Unauthorized member.").ShouldBeTrue();
        }

        [Fact]
        public async Task Approve_Proposal_ExpiredTime_Test()
        {
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
        }

        [Fact]
        public async Task Approve_Proposal_ApprovalAlreadyExists_Test()
        {
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
                await ParliamentContractStub.Approve.SendWithExceptionAsync(proposalId);
            transactionResult2.TransactionResult.Error.Contains("Already approved").ShouldBeTrue();
        }

        [Fact]
        public async Task Release_NotEnoughWeight_Test()
        {
            var minimalApprovalThreshold = 6667;
            var maximalAbstentionThreshold = 2000;
            var maximalRejectionThreshold = 3000;
            var minimalVoteThreshold = 8000;
            var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
                maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);
            var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
            await TransferToOrganizationAddressAsync(organizationAddress);
            await ApproveAsync(InitialMinersKeyPairs[0], proposalId);

            ParliamentContractStub = GetParliamentContractTester(DefaultSenderKeyPair);
            var result = await ParliamentContractStub.Release.SendWithExceptionAsync(proposalId);
            //Reviewer Shares < ReleaseThreshold, release failed
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.Contains("Not approved.").ShouldBeTrue();
        }

        [Fact]
        public async Task Release_NotFound_Test()
        {
            var proposalId = Hash.FromString("test");
            var result = await ParliamentContractStub.Release.SendWithExceptionAsync(proposalId);
            //Proposal not found
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.Contains("Proposal not found.").ShouldBeTrue();
        }

        [Fact]
        public async Task Release_WrongSender_Test()
        {
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
            result.TransactionResult.Error.Contains("Unable to release this proposal.").ShouldBeTrue();
        }

        [Fact]
        public async Task Release_Proposal_Test()
        {
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

            //After release,the proposal will be deleted
            //var getProposal = await AssociationAuthContractStub.GetProposal.SendAsync(proposalId.Result);
            //getProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            //getProposal.TransactionResult.Error.Contains("Not found proposal.").ShouldBeTrue();

            // Check inline transaction result
            var getBalance = TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = "ELF",
                Owner = Tester
            }).Result.Balance;
            getBalance.ShouldBe(100);
        }

        [Fact]
        public async Task Release_Proposal_AlreadyReleased_Test()
        {
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
        public async Task Change_GenesisContractOwner_Test()
        {
            var callResult = await ParliamentContractStub.GetDefaultOrganizationAddress.CallWithExceptionAsync(new Empty());
            callResult.Value.ShouldContain("Not initialized.");
            
            var initializeParliament = await ParliamentContractStub.Initialize.SendAsync(new InitializeInput
            {
                ProposerAuthorityRequired = false
            });
            initializeParliament.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var contractOwner = await ParliamentContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());
            contractOwner.ShouldNotBe(new Address());
            
            //no permission
            var transactionResult = await BasicContractStub.ChangeGenesisOwner.SendWithExceptionAsync(Tester);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.ShouldContain("Unauthorized behavior");
        }

        [Fact]
        public async Task Check_ValidProposal_Test()
        {
            var minimalApprovalThreshold = 6667;
            var maximalAbstentionThreshold = 2000;
            var maximalRejectionThreshold = 3000;
            var minimalVoteThreshold = 8000;
            var organizationAddress = await CreateOrganizationAsync(minimalApprovalThreshold,
                maximalAbstentionThreshold, maximalRejectionThreshold, minimalVoteThreshold);
            var proposalId = await CreateProposalAsync(DefaultSenderKeyPair, organizationAddress);
            await TransferToOrganizationAddressAsync(organizationAddress);
            
            //Get valid Proposal
            GetParliamentContractTester(InitialMinersKeyPairs.Last());
            var validProposals = await ParliamentContractStub.GetNotVotedPendingProposals.CallAsync(new ProposalIdList
            {
                ProposalIds = {proposalId}
            });
            validProposals.ProposalIds.Count.ShouldBe(1);
            
            await ApproveAsync(InitialMinersKeyPairs[0], proposalId);
            validProposals = await ParliamentContractStub.GetNotVotedPendingProposals.CallAsync(new ProposalIdList
            {
                ProposalIds = {proposalId}
            });
            validProposals.ProposalIds.Count.ShouldBe(0);
        }

        private async Task<Hash> CreateProposalAsync(ECKeyPair proposalKeyPair, Address organizationAddress)
        {
            var transferInput = new TransferInput()
            {
                Symbol = "ELF",
                Amount = 100,
                To = Tester,
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
            ParliamentContractStub = GetParliamentContractTester(proposalKeyPair);
            var proposal = await ParliamentContractStub.CreateProposal.SendAsync(createProposalInput);
            proposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var proposalCreated = ProposalCreated.Parser.ParseFrom(proposal.TransactionResult.Logs[0].NonIndexed)
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
            var transactionResult =
                await ParliamentContractStub.CreateOrganization.SendAsync(createOrganizationInput);
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
            ParliamentContractStub = GetParliamentContractTester(reviewer);
            var transactionResult =
                await ParliamentContractStub.Approve.SendAsync(proposalId);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }
    }
}