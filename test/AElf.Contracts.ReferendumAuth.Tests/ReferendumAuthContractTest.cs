using System;
using System.Threading;
using System.Threading.Tasks;
using Acs3;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestKit;
using AElf.Kernel;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using ApproveInput = Acs3.ApproveInput;

namespace AElf.Contracts.ReferendumAuth
{
    public class ReferendumAuthContractTest : ReferendumAuthContractTestBase
    {
        private CreateOrganizationInput _createOrganizationInput = new CreateOrganizationInput();
        private CreateProposalInput _createProposalInput = new CreateProposalInput();
        private CreateInput _createInput = new CreateInput();
        private Address _organizationAddress;

        public ReferendumAuthContractTest()
        {
            InitializeContracts();
        }

        [Fact]
        public async Task ReferendumAuthContract_InitializeMultiTimes()
        {
            var transactionResult =
                await ReferendumAuthContractStub.Initialize.SendAsync(new ReferendumAuthContractInitializationInput
                {
                    TokenContractSystemName = Hash.FromString("Test")
                });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("Already initialized.").ShouldBeTrue();
        }
        
        [Fact]
        public async Task Get_Organization()
        {
            _organizationAddress = await Create_Organization();
            var getOrganization = await ReferendumAuthContractStub.GetOrganization.CallAsync(_organizationAddress);
            getOrganization.OrganizationAddress.ShouldBe(_organizationAddress);
            getOrganization.ReleaseThreshold.ShouldBe(500000);
            getOrganization.OrganizationHash.ShouldBe(Hash.FromTwoHashes(
                Hash.FromMessage(ReferendumAuthContractAddress), Hash.FromMessage(_createOrganizationInput)));
        }
        
        [Fact]
        public async Task Get_OrganizationFailed()
        {
            var transactionResult =
                await ReferendumAuthContractStub.GetOrganization.SendAsync(Address.FromString("Test"));
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("No registered organization.").ShouldBeTrue();
        }
        
        [Fact]
        public async Task Get_Proposal()
        {
            _organizationAddress = await Create_Organization();
            var proposalId = await Create_Proposal();
            var getProposal = await ReferendumAuthContractStub.GetProposal.SendAsync(proposalId);
            getProposal.Output.Proposer.ShouldBe(DefaultSender);
            getProposal.Output.ContractMethodName.ShouldBe(nameof(TokenContract.Create));
            getProposal.Output.ProposalId.ShouldBe(proposalId);
            getProposal.Output.OrganizationAddress.ShouldBe(_organizationAddress);
            getProposal.Output.ToAddress.ShouldBe(TokenContractAddress);
            getProposal.Output.Params.ShouldBe(_createInput.ToByteString());
        }
        
        [Fact]
        public async Task Get_ProposalFailed()
        {
            
            var transactionResult = await ReferendumAuthContractStub.GetProposal.SendAsync(Hash.FromString("Test"));
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("Not found proposal.").ShouldBeTrue();
        }
        
        [Fact]
        public async Task Create_ProposalFailed()
        {
            _organizationAddress = await Create_Organization();
            var blockTime = BlockTimeProvider.GetBlockTime();
            _createProposalInput = new CreateProposalInput
            {
                ToAddress = Address.FromString("Test"),
                Params = ByteString.CopyFromUtf8("Test"),
                ExpiredTime = blockTime.AddDays(1).ToTimestamp(),
                OrganizationAddress =_organizationAddress
            };
            {
                //"Invalid proposal."
                var transactionResult = await ReferendumAuthContractStub.CreateProposal.SendAsync(_createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
            }
            {
                _createProposalInput.ContractMethodName = "Test";
                _createProposalInput.ToAddress = null;
                var transactionResult = await ReferendumAuthContractStub.CreateProposal.SendAsync(_createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
            }
            {
                _createProposalInput.ExpiredTime = null;
                _createProposalInput.ToAddress = Address.FromString("Test");
                var transactionResult = await ReferendumAuthContractStub.CreateProposal.SendAsync(_createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
            }
            {
                //"Expired proposal."
                
                _createProposalInput.ExpiredTime = blockTime.AddMilliseconds(5).ToTimestamp();
                Thread.Sleep(10);
                var transactionResult = await ReferendumAuthContractStub.CreateProposal.SendAsync(_createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Expired proposal.").ShouldBeTrue();
            }
            {
                //"No registered organization."
                _createProposalInput.ExpiredTime = BlockTimeProvider.GetBlockTime().AddDays(1).ToTimestamp();
                _createProposalInput.OrganizationAddress = Address.FromString("NoRegisteredOrganizationAddress");
                var transactionResult = await ReferendumAuthContractStub.CreateProposal.SendAsync(_createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("No registered organization.").ShouldBeTrue();
            }
            {
                //"Proposal already exists."
                _createProposalInput.OrganizationAddress = _organizationAddress;
                var transactionResult1 = await ReferendumAuthContractStub.CreateProposal.SendAsync(_createProposalInput);
                transactionResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                var transactionResult2 = await ReferendumAuthContractStub.CreateProposal.SendAsync(_createProposalInput);
                transactionResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult2.TransactionResult.Error.Contains("Proposal already exists.").ShouldBeTrue();
            }
        }
        
        [Fact]
        public async Task Approve_Proposal_NotFoundProposal()
        {
            var transactionResult = await ReferendumAuthContractStub.Approve.SendAsync(new ApproveInput
            {
                ProposalId = Hash.FromString("Test")
            });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("Proposal not found.").ShouldBeTrue();
        }

        [Fact]
        public async Task Approve_Proposal_MultiTimes()
        {
            _organizationAddress = await Create_Organization();
            var proposalId = await Create_Proposal();
            ReferendumAuthContractStub = GetReferendumAuthContractTester(SampleECKeyPairs.KeyPairs[1]);
            await ApproveToken(SampleECKeyPairs.KeyPairs[1],"ELF",_organizationAddress,2000);
            var transactionResult1 = await ReferendumAuthContractStub.Approve.SendAsync(new ApproveInput
            {
                ProposalId = proposalId,
                Quantity = 1000
            });
            Thread.Sleep(100);
            transactionResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var userBalance = await GetBalanceAsync("ELF", Address.FromPublicKey(SampleECKeyPairs.KeyPairs[1].PublicKey));
            userBalance.ShouldBe(10000 - 1000);
            
            var transactionResult2 = await ReferendumAuthContractStub.Approve.SendAsync(new ApproveInput
            {
                ProposalId = proposalId,
                Quantity = 1000
            });
            transactionResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult2.TransactionResult.Error.Contains("Cannot approve more than once.").ShouldBeTrue();
        }
        
        
        public async Task<Hash> Create_Proposal()
        {
            _organizationAddress = await Create_Organization();
            _createInput = new CreateInput()
            {
                Symbol = "NEW",
                Decimals = 2,
                TotalSupply = 10_0000,
                TokenName = "new token",
                Issuer = _organizationAddress,
                IsBurnable = true
            };
            _createProposalInput = new CreateProposalInput
            {
                ContractMethodName = nameof(TokenContract.Create),
                ToAddress = TokenContractAddress,
                Params = _createInput.ToByteString(),
                ExpiredTime = BlockTimeProvider.GetBlockTime().AddDays(2).ToTimestamp(),
                OrganizationAddress = _organizationAddress
            };
            var proposal = await ReferendumAuthContractStub.CreateProposal.SendAsync(_createProposalInput);
            proposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            return proposal.Output;
        }
        
        public async Task<Address> Create_Organization()
        {           
            _createOrganizationInput =  new CreateOrganizationInput
            {
                ReleaseThreshold = 500000,
                TokenSymbol = "ELF",
            };
            var transactionResult =
                await ReferendumAuthContractStub.CreateOrganization.SendAsync(_createOrganizationInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            return transactionResult.Output;
        }
        

    }
}