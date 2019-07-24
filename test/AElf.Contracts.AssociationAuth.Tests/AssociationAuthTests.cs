using System.Threading;
using System.Threading.Tasks;
using Acs3;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;
using ApproveInput = Acs3.ApproveInput;

namespace AElf.Contracts.AssociationAuth
{
    public class AssociationAuthTests : AssociationAuthContractTestBase
    {
        public AssociationAuthTests()
        {
            DeployContracts();
        }

        [Fact]
        public async Task Get_Organization()
        {
            var reviewer1 = new Reviewer {Address = Reviewer1, Weight = 1};
            var reviewer2 = new Reviewer {Address = Reviewer2, Weight = 2};
            var reviewer3 = new Reviewer {Address = Reviewer3, Weight = 3};

            var createOrganizationInput = new CreateOrganizationInput
            {
                Reviewers = {reviewer1, reviewer2, reviewer3},
                ReleaseThreshold = 2,
                ProposerThreshold = 2
            };
            var transactionResult =
                await AssociationAuthContractStub.CreateOrganization.SendAsync(createOrganizationInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var organizationAddress = transactionResult.Output;
            var getOrganization = await AssociationAuthContractStub.GetOrganization.CallAsync(organizationAddress);

            getOrganization.OrganizationAddress.ShouldBe(organizationAddress);
            getOrganization.Reviewers[0].Address.ShouldBe(Reviewer1);
            getOrganization.Reviewers[0].Weight.ShouldBe(1);
            getOrganization.ProposerThreshold.ShouldBe(2);
            getOrganization.ReleaseThreshold.ShouldBe(2);
            getOrganization.OrganizationHash.ShouldBe(Hash.FromTwoHashes(
                Hash.FromMessage(AssociationAuthContractAddress), Hash.FromMessage(createOrganizationInput)));
        }

        [Fact]
        public async Task Get_OrganizationFailed()
        {
            var transactionResult =
                await AssociationAuthContractStub.GetOrganization.SendAsync(SampleAddress.AddressList[0]);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("No registered organization.").ShouldBeTrue();
        }

        [Fact]
        public async Task Get_Proposal()
        {
            var organizationAddress = await CreateOrganizationAsync();
            var transferInput = new TransferInput()
            {
                Symbol = "ELF",
                Amount = 100,
                To = Reviewer1,
                Memo = "Transfer"
            };
            var proposalId = await CreateProposalAsync(Reviewer2KeyPair,organizationAddress);
            var getProposal = await AssociationAuthContractStub.GetProposal.SendAsync(proposalId);

            getProposal.Output.Proposer.ShouldBe(Reviewer2);
            getProposal.Output.ContractMethodName.ShouldBe(nameof(TokenContract.Transfer));
            getProposal.Output.ProposalId.ShouldBe(proposalId);
            getProposal.Output.OrganizationAddress.ShouldBe(organizationAddress);
            getProposal.Output.ToAddress.ShouldBe(TokenContractAddress);
            getProposal.Output.Params.ShouldBe(transferInput.ToByteString());
        }

        [Fact]
        public async Task Get_ProposalFailed()
        {
            var transactionResult = await AssociationAuthContractStub.GetProposal.SendAsync(Hash.FromString("Test"));
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("Not found proposal.").ShouldBeTrue();
        }

        [Fact]
        public async Task Create_OrganizationFailed()
        {
            var reviewer1 = new Reviewer {Address = Reviewer1, Weight = 1};
            var reviewer2 = new Reviewer {Address = Reviewer2, Weight = 2};
            var reviewer3 = new Reviewer {Address = Reviewer3, Weight = 3};

            var createOrganizationInput = new CreateOrganizationInput
            {
                Reviewers = {reviewer1, reviewer2, reviewer3},
                ReleaseThreshold = 2,
                ProposerThreshold = 2
            };
            //isValidWeight
            {
                createOrganizationInput.Reviewers[0].Weight = -1;
                var transactionResult =
                    await AssociationAuthContractStub.CreateOrganization.SendAsync(createOrganizationInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
            }
            //canBeProposed
            {
                createOrganizationInput.Reviewers[0].Weight = 1;
                createOrganizationInput.ProposerThreshold = 10;
                var transactionResult =
                    await AssociationAuthContractStub.CreateOrganization.SendAsync(createOrganizationInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
            }
            //canBeReleased
            {
                createOrganizationInput.ProposerThreshold = 2;
                createOrganizationInput.ReleaseThreshold = 10;
                var transactionResult =
                    await AssociationAuthContractStub.CreateOrganization.SendAsync(createOrganizationInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
            }
        }

        [Fact]
        public async Task Create_ProposalFailed()
        {
            var organizationAddress = await CreateOrganizationAsync();
            AssociationAuthContractStub = GetAssociationAuthContractTester(Reviewer2KeyPair);
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
                var transactionResult = await AssociationAuthContractStub.CreateProposal.SendAsync(createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
            }
            //ToAddress is null
            {
                createProposalInput.ContractMethodName = "Test";
                createProposalInput.ToAddress = null;

                var transactionResult = await AssociationAuthContractStub.CreateProposal.SendAsync(createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
            }
            //ExpiredTime is null
            {
                createProposalInput.ExpiredTime = null;
                createProposalInput.ToAddress = SampleAddress.AddressList[0];

                var transactionResult = await AssociationAuthContractStub.CreateProposal.SendAsync(createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
            }
            //"Expired proposal."
            {
                createProposalInput.ExpiredTime = blockTime.AddMilliseconds(5);
                Thread.Sleep(10);

                var transactionResult = await AssociationAuthContractStub.CreateProposal.SendAsync(createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Expired proposal.").ShouldBeTrue();
            }
            //"No registered organization."
            {
                createProposalInput.ExpiredTime = BlockTimeProvider.GetBlockTime().AddDays(1);
                createProposalInput.OrganizationAddress = SampleAddress.AddressList[1];

                var transactionResult = await AssociationAuthContractStub.CreateProposal.SendAsync(createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("No registered organization.").ShouldBeTrue();
            }
            //"Unable to propose."
            //Sender is not reviewer
            {
                createProposalInput.OrganizationAddress = organizationAddress;
                AssociationAuthContractStub = GetAssociationAuthContractTester(DefaultSenderKeyPair);

                var transactionResult =
                    await AssociationAuthContractStub.CreateProposal.SendAsync(createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Unable to propose.").ShouldBeTrue();
            }
            //Reviewer is not permission to propose
            {
                AssociationAuthContractStub = GetAssociationAuthContractTester(Reviewer1KeyPair);
                var transactionResult = await AssociationAuthContractStub.CreateProposal.SendAsync(createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Unable to propose.").ShouldBeTrue();
            }
            //"Proposal already exists."
            {
                AssociationAuthContractStub = GetAssociationAuthContractTester(Reviewer2KeyPair);
                var transactionResult1 =
                    await AssociationAuthContractStub.CreateProposal.SendAsync(createProposalInput);
                transactionResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var transactionResult2 =
                    await AssociationAuthContractStub.CreateProposal.SendAsync(createProposalInput);
                transactionResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult2.TransactionResult.Error.Contains("Proposal already exists.").ShouldBeTrue();
            }
        }

        [Fact]
        public async Task Approve_Proposal_NotFoundProposal()
        {
            var transactionResult = await AssociationAuthContractStub.Approve.SendAsync(new ApproveInput
            {
                ProposalId = Hash.FromString("Test")
            });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("Not found proposal.").ShouldBeTrue();
        }

        [Fact]
        public async Task Approve_Proposal_NotAuthorizedApproval()
        {
            var organizationAddress = await CreateOrganizationAsync();
            var proposalId = await CreateProposalAsync(Reviewer2KeyPair,organizationAddress);
            AssociationAuthContractStub = GetAssociationAuthContractTester(DefaultSenderKeyPair);
            var transactionResult = await AssociationAuthContractStub.Approve.SendAsync(new ApproveInput
            {
                ProposalId = proposalId
            });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("Not authorized approval.").ShouldBeTrue();
        }

        [Fact]
        public async Task Approve_Proposal_ExpiredTime()
        {
            var organizationAddress = await CreateOrganizationAsync();
            var proposalId = await CreateProposalAsync(Reviewer2KeyPair,organizationAddress);
            AssociationAuthContractStub = GetAssociationAuthContractTester(Reviewer1KeyPair);
            BlockTimeProvider.SetBlockTime(BlockTimeProvider.GetBlockTime().AddDays(5));
            var transactionResult = await AssociationAuthContractStub.Approve.CallAsync(new ApproveInput
            {
                ProposalId = proposalId
            });
            transactionResult.Value.ShouldBe(false);
        }

        [Fact]
        public async Task Approve_Proposal_ApprovalAlreadyExists()
        {
            var organizationAddress = await CreateOrganizationAsync();
            var proposalId = await CreateProposalAsync(Reviewer2KeyPair,organizationAddress);
            AssociationAuthContractStub = GetAssociationAuthContractTester(Reviewer1KeyPair);

            var transactionResult1 =
                await AssociationAuthContractStub.Approve.SendAsync(new ApproveInput {ProposalId = proposalId});
            transactionResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            transactionResult1.Output.Value.ShouldBe(true);

            Thread.Sleep(100);
            var transactionResult2 =
                await AssociationAuthContractStub.Approve.SendAsync(new ApproveInput {ProposalId = proposalId});
            transactionResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult2.TransactionResult.Error.Contains("Approval already exists.").ShouldBeTrue();
        }
        
        [Fact]
        public async Task Release_NotEnoughWeight()
        {
            var organizationAddress = await CreateOrganizationAsync();
            var proposalId = await CreateProposalAsync(Reviewer2KeyPair,organizationAddress);
            await TransferToOrganizationAddressAsync(organizationAddress);
            await ApproveAsync(Reviewer1KeyPair,proposalId);
  
            AssociationAuthContractStub = GetAssociationAuthContractTester(Reviewer2KeyPair);
            var result = await AssociationAuthContractStub.Release.SendAsync(proposalId);
            //Reviewer weight < ReleaseThreshold, release failed
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.Contains("Not approved.").ShouldBeTrue();
        }

        [Fact]
        public async Task Release_NotFound()
        { 
            var proposalId = Hash.FromString("test");
            AssociationAuthContractStub = GetAssociationAuthContractTester(Reviewer2KeyPair);
            var result = await AssociationAuthContractStub.Release.SendAsync(proposalId);
            //Proposal not found
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.Contains("Proposal not found.").ShouldBeTrue();
        }

        [Fact]
        public async Task Release_WrongSender()
        {
            var organizationAddress = await CreateOrganizationAsync();
            var proposalId = await CreateProposalAsync(Reviewer2KeyPair,organizationAddress);
            await TransferToOrganizationAddressAsync(organizationAddress);
            await ApproveAsync(Reviewer3KeyPair,proposalId);
  
            AssociationAuthContractStub = GetAssociationAuthContractTester(Reviewer1KeyPair);
            var result = await AssociationAuthContractStub.Release.SendAsync(proposalId);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.Contains("Unable to release this proposal.").ShouldBeTrue();
        }

        [Fact]
        public async Task Release_Proposal()
        {
            var organizationAddress = await CreateOrganizationAsync();
            var proposalId = await CreateProposalAsync(Reviewer2KeyPair,organizationAddress);
            await TransferToOrganizationAddressAsync(organizationAddress);
            await ApproveAsync(Reviewer3KeyPair,proposalId);
  
            AssociationAuthContractStub = GetAssociationAuthContractTester(Reviewer2KeyPair);
            var result = await AssociationAuthContractStub.Release.SendAsync(proposalId);
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            //After release,the proposal will be deleted
            //var getProposal = await AssociationAuthContractStub.GetProposal.SendAsync(proposalId.Result);
            //getProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            //getProposal.TransactionResult.Error.Contains("Not found proposal.").ShouldBeTrue();
            
            // Check inline transaction result
            var getBalance = TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = "ELF",
                Owner = Reviewer1
            }).Result.Balance;
            getBalance.ShouldBe(100);
        }

        private async Task<Hash> CreateProposalAsync(ECKeyPair proposalKeyPair,Address organizationAddress)
        {
            var transferInput = new TransferInput()
            {
                Symbol = "ELF",
                Amount = 100,
                To = Reviewer1,
                Memo = "Transfer"
            };
            AssociationAuthContractStub = GetAssociationAuthContractTester(proposalKeyPair);
            var createProposalInput = new CreateProposalInput
            {
                ContractMethodName = nameof(TokenContract.Transfer),
                ToAddress = TokenContractAddress,
                Params = transferInput.ToByteString(),
                ExpiredTime = BlockTimeProvider.GetBlockTime().AddDays(2),
                OrganizationAddress = organizationAddress
            };
            var proposal = await AssociationAuthContractStub.CreateProposal.SendAsync(createProposalInput);
            return proposal.Output;
        }

        private async Task<Address> CreateOrganizationAsync()
        {
            var reviewer1 = new Reviewer {Address = Reviewer1, Weight = 1};
            var reviewer2 = new Reviewer {Address = Reviewer2, Weight = 2};
            var reviewer3 = new Reviewer {Address = Reviewer3, Weight = 3};

            var createOrganizationInput = new CreateOrganizationInput
            {
                Reviewers = {reviewer1, reviewer2, reviewer3},
                ReleaseThreshold = 2,
                ProposerThreshold = 2
            };
            var transactionResult =
                await AssociationAuthContractStub.CreateOrganization.SendAsync(createOrganizationInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            return transactionResult.Output;
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
        
        private async Task ApproveAsync(ECKeyPair reviewer, Hash proposalId )
        {
            AssociationAuthContractStub = GetAssociationAuthContractTester(reviewer);

            var transactionResult1 =
                await AssociationAuthContractStub.Approve.SendAsync(new ApproveInput {ProposalId = proposalId});
            transactionResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            transactionResult1.Output.Value.ShouldBe(true);
        }
    }
}