using System.Threading;
using System.Threading.Tasks;
using Acs3;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.AssociationAuth
{
    public class AssociationAuthTests : AssociationAuthContractTestBase
    {
        private CreateOrganizationInput _createOrganizationInput = new CreateOrganizationInput();
        private CreateProposalInput _createProposalInput = new CreateProposalInput();
        private Address _organizationAddress;
        public AssociationAuthTests() {
            DeployContracts();
        }
        
        [Fact]
        public async Task Get_Organization()
        {
            _organizationAddress = Create_Organization().Result;
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
                await AssociationAuthContractStub.GetOrganization.SendAsync(Address.Generate());
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("No registered organization.").ShouldBeTrue();
        }

        [Fact]
        public async Task Get_Proposal()
        {
            var proposalId = Create_Proposal();
            var getProposal = await AssociationAuthContractStub.GetProposal.SendAsync(proposalId.Result);
            getProposal.Output.Proposer.ShouldBe(Reviewer2);
            getProposal.Output.ContractMethodName.ShouldBe("Test");
            getProposal.Output.CanBeReleased.ShouldBe(false);
            getProposal.Output.ProposalId.ShouldBe(proposalId.Result);
            getProposal.Output.OrganizationAddress.ShouldBe(_organizationAddress);
            getProposal.Output.ToAddress.ShouldBe(Address.FromString("Test"));
            getProposal.Output.Params.ShouldBe(ByteString.CopyFromUtf8("Test"));
        }
        
        [Fact]
        public async Task Get_ProposalFailed()
        {
            var transactionResult = await AssociationAuthContractStub.GetProposal.SendAsync(Hash.Generate());
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("Invalid proposal Id.").ShouldBeTrue();
        }

        [Fact]
        public async Task Create_ProposalFailed()
        {
            _organizationAddress = Create_Organization().Result;
            AssociationAuthContractStub = GetAssociationAuthContractTester(Reviewer2KeyPair);
            _createProposalInput = new CreateProposalInput
            {
                ToAddress = Address.FromString("Test"),
                Params = ByteString.CopyFromUtf8("Test"),
                ExpiredTime = BlockTimeProvider.GetBlockTime().AddDays(1).ToTimestamp(),
                OrganizationAddress = _organizationAddress
            };
            {
                //"Invalid proposal."
                var transactionResult = await AssociationAuthContractStub.CreateProposal.SendAsync(_createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
            }
            {
                _createProposalInput.ContractMethodName = "Test";
                _createProposalInput.ToAddress = null;
                var transactionResult = await AssociationAuthContractStub.CreateProposal.SendAsync(_createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
            }
            {
                _createProposalInput.ExpiredTime = null;
                _createProposalInput.ToAddress = Address.FromString("Test");
                var transactionResult = await AssociationAuthContractStub.CreateProposal.SendAsync(_createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
            }
            {
                //"Expired proposal."
                var blockTime = BlockTimeProvider.GetBlockTime();
                _createProposalInput.ExpiredTime = blockTime.AddMilliseconds(5).ToTimestamp();
                Thread.Sleep(5);
                var transactionResult = await AssociationAuthContractStub.CreateProposal.SendAsync(_createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Expired proposal.").ShouldBeTrue();
            }
            {
                //"No registered organization."
                _createProposalInput.ExpiredTime = BlockTimeProvider.GetBlockTime().AddDays(1).ToTimestamp();
                _createProposalInput.OrganizationAddress = Address.FromString("NoRegisteredOrganizationAddress");
                var transactionResult = await AssociationAuthContractStub.CreateProposal.SendAsync(_createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("No registered organization.").ShouldBeTrue();
            }
            {
                //"Unable to propose."
                _createProposalInput.OrganizationAddress = _organizationAddress;
                AssociationAuthContractStub = GetAssociationAuthContractTester(DefaultSenderKeyPair);

                var transactionResult = await AssociationAuthContractStub.CreateProposal.SendAsync(_createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Unable to propose.").ShouldBeTrue();
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
        }



        public async Task<Hash> Create_Proposal()
        {
            _organizationAddress = Create_Organization().Result;
            AssociationAuthContractStub = GetAssociationAuthContractTester(Reviewer2KeyPair);
            _createProposalInput = new CreateProposalInput
            {
                ContractMethodName = "Test",
                ToAddress = Address.FromString("Test"),
                Params = ByteString.CopyFromUtf8("Test"),
                ExpiredTime = BlockTimeProvider.GetBlockTime().AddDays(1).ToTimestamp(),
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
    }
}