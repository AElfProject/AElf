using System;
using System.Threading;
using System.Threading.Tasks;
using Acs3;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Contracts.TestKit;
using AElf.Kernel;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using ApproveInput = Acs3.ApproveInput;

namespace AElf.Contracts.ReferendumAuth
{
    public class ReferendumAuthContractTest : ReferendumAuthContractTestBase
    {
        public ReferendumAuthContractTest()
        {
            InitializeContracts();
        }

        [Fact]
        public async Task ReferendumAuthContract_InitializeMultiTimes()
        {
            var transactionResult =
                await ReferendumAuthContractStub.Initialize.SendAsync(new Empty());
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("Already initialized.").ShouldBeTrue();
        }
        
        [Fact]
        public async Task Get_Organization()
        {
            var createOrganizationInput =  new CreateOrganizationInput
            {
                ReleaseThreshold = 5000,
                TokenSymbol = "ELF",
            };
            var organizationAddress = await Create_Organization();
            var getOrganization = await ReferendumAuthContractStub.GetOrganization.CallAsync(organizationAddress);
            
            getOrganization.OrganizationAddress.ShouldBe(organizationAddress);
            getOrganization.ReleaseThreshold.ShouldBe(5000);
            getOrganization.OrganizationHash.ShouldBe(Hash.FromTwoHashes(
                Hash.FromMessage(ReferendumAuthContractAddress), Hash.FromMessage(createOrganizationInput)));
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
            var organizationAddress = await Create_Organization();
            var createInput = new CreateInput()
            {
                Symbol = "NEW",
                Decimals = 2,
                TotalSupply = 10_0000,
                TokenName = "new token",
                Issuer = organizationAddress,
                IsBurnable = true
            };
            var proposalId = await Create_Proposal();
            var getProposal = await ReferendumAuthContractStub.GetProposal.SendAsync(proposalId);
            
            getProposal.Output.Proposer.ShouldBe(DefaultSender);
            getProposal.Output.ContractMethodName.ShouldBe(nameof(TokenContract.Create));
            getProposal.Output.ProposalId.ShouldBe(proposalId);
            getProposal.Output.OrganizationAddress.ShouldBe(organizationAddress);
            getProposal.Output.ToAddress.ShouldBe(TokenContractAddress);
            getProposal.Output.Params.ShouldBe(createInput.ToByteString());
        }
        
        [Fact]
        public async Task Get_ProposalFailed()
        {
            var transactionResult = await ReferendumAuthContractStub.GetProposal.SendAsync(Hash.FromString("Test"));
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("Proposal not found.").ShouldBeTrue();
        }
        
        [Fact]
        public async Task Create_ProposalFailed()
        {
            var organizationAddress = await Create_Organization();
            var blockTime = BlockTimeProvider.GetBlockTime();
            var createProposalInput = new CreateProposalInput
            {
                ToAddress = Address.FromString("Test"),
                Params = ByteString.CopyFromUtf8("Test"),
                ExpiredTime = blockTime.AddDays(1),
                OrganizationAddress =organizationAddress
            };
            {
                //"Invalid proposal."
                var transactionResult = await ReferendumAuthContractStub.CreateProposal.SendAsync(createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
            }
            {
                createProposalInput.ContractMethodName = "Test";
                createProposalInput.ToAddress = null;
                
                var transactionResult = await ReferendumAuthContractStub.CreateProposal.SendAsync(createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
            }
            {
                createProposalInput.ExpiredTime = null;
                createProposalInput.ToAddress = Address.FromString("Test");
                
                var transactionResult = await ReferendumAuthContractStub.CreateProposal.SendAsync(createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
            }
            {
                //"Expired proposal."
                createProposalInput.ExpiredTime = blockTime.AddMilliseconds(5);
                Thread.Sleep(10);
                
                var transactionResult = await ReferendumAuthContractStub.CreateProposal.SendAsync(createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Expired proposal.").ShouldBeTrue();
            }
            {
                //"No registered organization."
                createProposalInput.ExpiredTime = BlockTimeProvider.GetBlockTime().AddDays(1);
                createProposalInput.OrganizationAddress = Address.FromString("NoRegisteredOrganizationAddress");
                
                var transactionResult = await ReferendumAuthContractStub.CreateProposal.SendAsync(createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("No registered organization.").ShouldBeTrue();
            }
            {
                //"Proposal already exists."
                createProposalInput.OrganizationAddress = organizationAddress;
                var transactionResult1 = await ReferendumAuthContractStub.CreateProposal.SendAsync(createProposalInput);
                transactionResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                
                var transactionResult2 = await ReferendumAuthContractStub.CreateProposal.SendAsync(createProposalInput);
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
            var proposalId = await Create_Proposal();
            ReferendumAuthContractStub = GetReferendumAuthContractTester(SampleECKeyPairs.KeyPairs[1]);
            var transactionResult1 = await ReferendumAuthContractStub.Approve.SendAsync(new ApproveInput
            {
                ProposalId = proposalId,
                Quantity = 1000
            });
            transactionResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var userBalance = await GetBalanceAsync("ELF", Address.FromPublicKey(SampleECKeyPairs.KeyPairs[1].PublicKey));
            userBalance.ShouldBe(10000 - 1000);
            
            Thread.Sleep(100);
            var transactionResult2 = await ReferendumAuthContractStub.Approve.SendAsync(new ApproveInput
            {
                ProposalId = proposalId,
                Quantity = 1000
            });
            transactionResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult2.TransactionResult.Error.Contains("Cannot approve more than once.").ShouldBeTrue();
        }
        
        [Fact]
        public async Task Approve_Proposal_ExpiredTime()
        {
            var proposalId = await Create_Proposal();
            
            ReferendumAuthContractStub = GetReferendumAuthContractTester(SampleECKeyPairs.KeyPairs[1]);
            BlockTimeProvider.SetBlockTime(BlockTimeProvider.GetBlockTime().AddDays(5));
            
            var transactionResult = await ReferendumAuthContractStub.Approve.CallAsync(new ApproveInput
            {
                ProposalId = proposalId
            });
            transactionResult.Value.ShouldBe(false);
        }
        
        [Fact]
        public async Task Approve_InvalidVote()
        {
            var proposalId = await Create_Proposal();
            
            ReferendumAuthContractStub = GetReferendumAuthContractTester(SampleECKeyPairs.KeyPairs[1]);
            var transactionResult = await ReferendumAuthContractStub.Approve.SendAsync(new ApproveInput
            {
                ProposalId = proposalId,
                Quantity = 0
            });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("Invalid vote.").ShouldBeTrue();
        }
        
        [Fact]
        public async Task Approve_And_ReleaseProposal_1()
        {
            var createOrganizationInput =  new CreateOrganizationInput
            {
                ReleaseThreshold = 5000,
                TokenSymbol = "ELF",
            };
            
            var organizationAddress = await Create_Organization();
            var proposalId = await Create_Proposal();
            
             ReferendumAuthContractStub = GetReferendumAuthContractTester(SampleECKeyPairs.KeyPairs[2]);  
             var transactionResult = await ReferendumAuthContractStub.Approve.SendAsync(new ApproveInput
             {
                    ProposalId = proposalId,
                    Quantity = createOrganizationInput.ReleaseThreshold
             });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            transactionResult.Output.Value.ShouldBe(true);
               
//            After release,the proposal will be deleted
//            var getProposal = await ReferendumAuthContractStub.GetProposal.SendAsync(proposalId.Result);
//            getProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
//            getProposal.TransactionResult.Error.Contains("Not found proposal.").ShouldBeTrue();
            
            //Check the inline transaction
            var newToken = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput{Symbol = "NEW"});
            newToken.Issuer.ShouldBe(organizationAddress);
        }

        [Fact]
        public async Task Approve_And_ReleaseProposal_2()
        {
            var createOrganizationInput =  new CreateOrganizationInput
            {
                ReleaseThreshold = 5000,
                TokenSymbol = "ELF",
            };
            var organizationAddress = await Create_Organization();
            var proposalId = await Create_Proposal();

            for (int i = 1; i < 6; i++)
            {
                ReferendumAuthContractStub = GetReferendumAuthContractTester(SampleECKeyPairs.KeyPairs[i]);  
                var transactionResult = await ReferendumAuthContractStub.Approve.SendAsync(new ApproveInput
                {
                    ProposalId = proposalId,
                    Quantity = createOrganizationInput.ReleaseThreshold / 5
                });
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                transactionResult.Output.Value.ShouldBe(true);
            }
           
//            After release,the proposal will be deleted
//            var getProposal = await ReferendumAuthContractStub.GetProposal.SendAsync(proposalId.Result);
//            getProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
//            getProposal.TransactionResult.Error.Contains("Not found proposal.").ShouldBeTrue();
            
            //Check the inline transaction
            var newToken = await TokenContractStub.GetTokenInfo.CallAsync(new GetTokenInfoInput{Symbol = "NEW"});
            newToken.Issuer.ShouldBe(organizationAddress);
        }
        
        // TODO: after release proposal can't reclaim token.
        [Fact]
        public async Task Reclaim_VoteTokenFailed()
        {
            var proposalId = await Create_Proposal();
            
            ReferendumAuthContractStub = GetReferendumAuthContractTester(SampleECKeyPairs.KeyPairs[1]);  
            var transactionResult = await ReferendumAuthContractStub.Approve.SendAsync(new ApproveInput
            {
                ProposalId = proposalId,
                Quantity = 1
            });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            transactionResult.Output.Value.ShouldBe(true);
            
            var reclaimResult = await ReferendumAuthContractStub.ReclaimVoteToken.SendAsync(proposalId);
            reclaimResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            reclaimResult.TransactionResult.Error.Contains("Unable to reclaim at this time.").ShouldBeTrue();
        }
        
        [Fact]
        public async Task Reclaim_VoteTokenWithoutVote()
        {
            var proposalId = await Create_Proposal();
            
            ReferendumAuthContractStub = GetReferendumAuthContractTester(SampleECKeyPairs.KeyPairs[1]);  
            var reclaimResult = await ReferendumAuthContractStub.ReclaimVoteToken.SendAsync(proposalId);
            reclaimResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            reclaimResult.TransactionResult.Error.Contains("Nothing to reclaim.").ShouldBeTrue();
        }
        
        public async Task<Hash> Create_Proposal()
        {
            var organizationAddress = await Create_Organization();
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
                ExpiredTime = BlockTimeProvider.GetBlockTime().AddDays(2),
                OrganizationAddress = organizationAddress
            };
            var proposal = await ReferendumAuthContractStub.CreateProposal.SendAsync(createProposalInput);
            proposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            return proposal.Output;
        }
        
        public async Task<Address> Create_Organization()
        {           
            var createOrganizationInput =  new CreateOrganizationInput
            {
                ReleaseThreshold = 5000,
                TokenSymbol = "ELF",
            };
            var transactionResult =
                await ReferendumAuthContractStub.CreateOrganization.SendAsync(createOrganizationInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            return transactionResult.Output;
        }
    }
}