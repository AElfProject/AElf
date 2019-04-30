using System.Threading;
using System.Threading.Tasks;
using Acs3;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using ApproveInput = Acs3.ApproveInput;

namespace AElf.Contracts.AssociationAuth
{
    public class AssociationAuthTests : AssociationAuthContractTestBase
    {
        private CreateOrganizationInput _createOrganizationInput = new CreateOrganizationInput();
        private CreateProposalInput _createProposalInput = new CreateProposalInput();
        private TransferInput _transferInput = new TransferInput();
        private Address _organizationAddress;
        public AssociationAuthTests() {
            DeployContracts();
        }
        
        [Fact]
        public async Task Get_Organization()
        {
            _organizationAddress = await Create_Organization();
            var getOrganization = await AssociationAuthContractStub.GetOrganization.CallAsync(_organizationAddress);
            
            getOrganization.OrganizationAddress.ShouldBe(_organizationAddress);
            getOrganization.Reviewers[0].Address.ShouldBe(Reviewer1);
            getOrganization.Reviewers[0].Weight.ShouldBe(1);
            getOrganization.ProposerThreshold.ShouldBe(2);
            getOrganization.ReleaseThreshold.ShouldBe(2);
            getOrganization.OrganizationHash.ShouldBe(Hash.FromTwoHashes(Hash.FromMessage(AssociationAuthContractAddress), Hash.FromMessage(_createOrganizationInput)));
        }

        [Fact]
        public async Task Get_OrganizationFailed()
        {
            var transactionResult =
                await AssociationAuthContractStub.GetOrganization.SendAsync(Address.FromString("Test"));
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("No registered organization.").ShouldBeTrue();
        }

        [Fact]
        public async Task Get_Proposal()
        {
            var proposalId = await Create_Proposal();
            var getProposal = await AssociationAuthContractStub.GetProposal.SendAsync(proposalId);
            
            getProposal.Output.Proposer.ShouldBe(Reviewer2);
            getProposal.Output.ContractMethodName.ShouldBe(nameof(TokenContract.Transfer));
            getProposal.Output.ProposalId.ShouldBe(proposalId);
            getProposal.Output.OrganizationAddress.ShouldBe(_organizationAddress);
            getProposal.Output.ToAddress.ShouldBe(TokenContractAddress);
            getProposal.Output.Params.ShouldBe(_transferInput.ToByteString());
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
            var reviewer1 = new Reviewer{Address = Reviewer1,Weight = 1};
            var reviewer2 = new Reviewer{Address = Reviewer2,Weight = 2};
            var reviewer3 = new Reviewer{Address = Reviewer3,Weight = 3};
            
            _createOrganizationInput =  new CreateOrganizationInput
            {
                Reviewers = {reviewer1,reviewer2,reviewer3},
                ReleaseThreshold = 2,
                ProposerThreshold = 2
            };
            //isValidWeight
            {
                _createOrganizationInput.Reviewers[0].Weight = -1;
                var transactionResult =
                    await AssociationAuthContractStub.CreateOrganization.SendAsync(_createOrganizationInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
            }
            //canBeProposed
            {
                _createOrganizationInput.Reviewers[0].Weight = 1;
                _createOrganizationInput.ProposerThreshold = 10;
                var transactionResult =
                    await AssociationAuthContractStub.CreateOrganization.SendAsync(_createOrganizationInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
            }
            //canBeReleased
            {
                _createOrganizationInput.ProposerThreshold = 2;
                _createOrganizationInput.ReleaseThreshold = 10;
                var transactionResult =
                    await AssociationAuthContractStub.CreateOrganization.SendAsync(_createOrganizationInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
            }
        }

        [Fact]
        public async Task Create_ProposalFailed()
        {
            _organizationAddress = await Create_Organization();
            AssociationAuthContractStub = GetAssociationAuthContractTester(Reviewer2KeyPair);
            var blockTime = BlockTimeProvider.GetBlockTime();
            _createProposalInput = new CreateProposalInput
            {
                ToAddress = Address.FromString("Test"),
                Params = ByteString.CopyFromUtf8("Test"),
                ExpiredTime = blockTime.AddDays(1).ToTimestamp(),
                OrganizationAddress = _organizationAddress
            };
            //"Invalid proposal."
            //ContractMethodName is null or white space
            {
                var transactionResult = await AssociationAuthContractStub.CreateProposal.SendAsync(_createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
            }
            //ToAddress is null
            {
                _createProposalInput.ContractMethodName = "Test";
                _createProposalInput.ToAddress = null;
                
                var transactionResult = await AssociationAuthContractStub.CreateProposal.SendAsync(_createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
            }
            //ExpiredTime is null
            {
                _createProposalInput.ExpiredTime = null;
                _createProposalInput.ToAddress = Address.FromString("Test");
                
                var transactionResult = await AssociationAuthContractStub.CreateProposal.SendAsync(_createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
            }
            //"Expired proposal."
            {
                _createProposalInput.ExpiredTime = blockTime.AddMilliseconds(5).ToTimestamp();
                Thread.Sleep(10);
                
                var transactionResult = await AssociationAuthContractStub.CreateProposal.SendAsync(_createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Expired proposal.").ShouldBeTrue();
            }
            //"No registered organization."
            {
                _createProposalInput.ExpiredTime = BlockTimeProvider.GetBlockTime().AddDays(1).ToTimestamp();
                _createProposalInput.OrganizationAddress = Address.FromString("NoRegisteredOrganizationAddress");
                
                var transactionResult = await AssociationAuthContractStub.CreateProposal.SendAsync(_createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("No registered organization.").ShouldBeTrue();
            }
            //"Unable to propose."
            //Sender is not reviewer
            {
                _createProposalInput.OrganizationAddress = _organizationAddress;
                AssociationAuthContractStub = GetAssociationAuthContractTester(DefaultSenderKeyPair);

                var transactionResult =
                    await AssociationAuthContractStub.CreateProposal.SendAsync(_createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Unable to propose.").ShouldBeTrue();
            }
            //Reviewer is not permission to propose
            {
                AssociationAuthContractStub = GetAssociationAuthContractTester(Reviewer1KeyPair);
                var transactionResult = await AssociationAuthContractStub.CreateProposal.SendAsync(_createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Unable to propose.").ShouldBeTrue();
            }
            //"Proposal already exists."
            {
                AssociationAuthContractStub = GetAssociationAuthContractTester(Reviewer2KeyPair);
                var transactionResult1 = await AssociationAuthContractStub.CreateProposal.SendAsync(_createProposalInput);
                transactionResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                
                var transactionResult2 = await AssociationAuthContractStub.CreateProposal.SendAsync(_createProposalInput);
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
            var proposalId = await Create_Proposal();
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
            var proposalId = await Create_Proposal();
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
            var proposalId = await Create_Proposal();
            AssociationAuthContractStub = GetAssociationAuthContractTester(Reviewer1KeyPair);
            
            var transactionResult1 = await AssociationAuthContractStub.Approve.SendAsync(new ApproveInput{ProposalId = proposalId});
            transactionResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            transactionResult1.Output.Value.ShouldBe(true);
            
            Thread.Sleep(100);
            var transactionResult2 = await AssociationAuthContractStub.Approve.SendAsync(new ApproveInput{ProposalId = proposalId});
            transactionResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult2.TransactionResult.Error.Contains("Approval already exists.").ShouldBeTrue();
        }

        [Fact]
        public async Task Approve_And_ReleaseProposal_1()
        {
            var proposalId = await Create_Proposal();
            await TransferForOrganizationAddress();
            AssociationAuthContractStub = GetAssociationAuthContractTester(Reviewer1KeyPair);
            
            var transactionResult1 = await AssociationAuthContractStub.Approve.SendAsync(new ApproveInput{ProposalId = proposalId});
            transactionResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            transactionResult1.Output.Value.ShouldBe(true);
            
            //Reviewer weight < ReleaseThreshold, don't release 
            var getBalance =TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = "ELF",
                Owner = Reviewer1
            }).Result.Balance;
            getBalance.ShouldBe(0);
            
            AssociationAuthContractStub = GetAssociationAuthContractTester(Reviewer2KeyPair);
            var transactionResult2 = await AssociationAuthContractStub.Approve.SendAsync(new ApproveInput{ProposalId = proposalId});
            transactionResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            transactionResult2.Output.Value.ShouldBe(true);
            
//            After release,the proposal will be deleted
//            var getProposal = await AssociationAuthContractStub.GetProposal.SendAsync(proposalId.Result);
//            getProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
//            getProposal.TransactionResult.Error.Contains("Not found proposal.").ShouldBeTrue();
                        
            getBalance =TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = "ELF",
                Owner = Reviewer1
            }).Result.Balance;
            getBalance.ShouldBe(100);
        }

        [Fact]
        public async Task Approve_And_ReleaseProposal_2()
        {
            var proposalId = await Create_Proposal();
            await TransferForOrganizationAddress();
            AssociationAuthContractStub = GetAssociationAuthContractTester(Reviewer3KeyPair);
            var transactionResult = await AssociationAuthContractStub.Approve.SendAsync(new ApproveInput{ProposalId = proposalId});
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            transactionResult.Output.Value.ShouldBe(true);
            
//            After release,the proposal will be deleted
//            var getProposal = await AssociationAuthContractStub.GetProposal.SendAsync(proposalId.Result);
//            getProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
//            getProposal.TransactionResult.Error.Contains("Not found proposal.").ShouldBeTrue();
            
            var getBalance =TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = "ELF",
                Owner = Reviewer1
            }).Result.Balance;
            getBalance.ShouldBe(100);
        }
        
        [Fact]
        public async Task Approve_And_ReleaseProposalFailed()
        {
            var proposalId = await Create_Proposal();
            AssociationAuthContractStub = GetAssociationAuthContractTester(Reviewer3KeyPair);
            var transactionResult = await AssociationAuthContractStub.Approve.SendAsync(new ApproveInput{ProposalId = proposalId});
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            
//            After release,the proposal will be deleted
//            var getProposal = await AssociationAuthContractStub.GetProposal.SendAsync(proposalId.Result);
//            getProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
//            getProposal.TransactionResult.Error.Contains("Not found proposal.").ShouldBeTrue();
                  
            var getBalance =TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = "ELF",
                Owner = Reviewer1
            }).Result.Balance;
            getBalance.ShouldBe(0);
        }


        public async Task<Hash> Create_Proposal()
        {
            _organizationAddress = await Create_Organization();
            
            _transferInput = new TransferInput()
            {
                Symbol = "ELF",
                Amount = 100,
                To = Reviewer1,
                Memo = "Transfer"
            };
            AssociationAuthContractStub = GetAssociationAuthContractTester(Reviewer2KeyPair);
            _createProposalInput = new CreateProposalInput
            {
                ContractMethodName = nameof(TokenContract.Transfer),
                ToAddress = TokenContractAddress,
                Params = _transferInput.ToByteString(),
                ExpiredTime = BlockTimeProvider.GetBlockTime().AddDays(2).ToTimestamp(),
                OrganizationAddress = _organizationAddress
            };
            var proposal = await AssociationAuthContractStub.CreateProposal.SendAsync(_createProposalInput);
            return proposal.Output;
        }

        public async Task<Address> Create_Organization()
        {
            var reviewer1 = new Reviewer{Address = Reviewer1,Weight = 1};
            var reviewer2 = new Reviewer{Address = Reviewer2,Weight = 2};
            var reviewer3 = new Reviewer{Address = Reviewer3,Weight = 3};
            
            _createOrganizationInput =  new CreateOrganizationInput
            {
                Reviewers = {reviewer1,reviewer2,reviewer3},
                ReleaseThreshold = 2,
                ProposerThreshold = 2
            };
            var transactionResult =
                await AssociationAuthContractStub.CreateOrganization.SendAsync(_createOrganizationInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            return transactionResult.Output;
        }

        public async Task TransferForOrganizationAddress()
        {
            await TokenContractStub.Transfer.SendAsync(new TransferInput
            {
                Symbol = "ELF",
                Amount = 200,
                To = _organizationAddress,
                Memo = "transfer organization address"
            });
        }
    }
}