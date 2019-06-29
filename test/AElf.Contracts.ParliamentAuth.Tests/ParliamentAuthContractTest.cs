using System.Threading;
using System.Threading.Tasks;
using Acs3;
using AElf.Contracts.MultiToken.Messages;
using AElf.Sdk.CSharp;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using ApproveInput = Acs3.ApproveInput;

namespace AElf.Contracts.ParliamentAuth
{
    public class ParliamentAuthContractTest : ParliamentAuthContractTestBase
    {
        private CreateOrganizationInput _createOrganizationInput = new CreateOrganizationInput();
        private CreateProposalInput _createProposalInput = new CreateProposalInput();
        private TransferInput _transferInput = new TransferInput();
        private Address _organizationAddress;
        private Address _defaultOrganizationAddress;
        
        public ParliamentAuthContractTest()
        {
            InitializeContracts();
        }

        [Fact]
        public async Task Get_DefaultOrganizationAddressFailed()
        {
            var transactionResult =
                await OtherParliamentAuthContractStub.GetDefaultOrganizationAddress.SendAsync(new Empty());
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("Not initialized.").ShouldBeTrue();
        }

        [Fact]
        public async Task ParliamentAuthContract_InitializeMultiTimes()
        {
            var transactionResult = (await ParliamentAuthContractStub.Initialize.SendAsync(new Empty())).TransactionResult;
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Already initialized.").ShouldBeTrue();
        }
        
        [Fact]
        public async Task Get_Organization()
        {
            _organizationAddress = await Create_Organization();
            var getOrganization = await ParliamentAuthContractStub.GetOrganization.CallAsync(_organizationAddress);
            
            getOrganization.OrganizationAddress.ShouldBe(_organizationAddress);
            getOrganization.ReleaseThreshold.ShouldBe(10000/MinersCount);
            getOrganization.OrganizationHash.ShouldBe(Hash.FromTwoHashes(
                Hash.FromMessage(ParliamentAuthContractAddress), Hash.FromMessage(_createOrganizationInput)));
        }

        [Fact]
        public async Task Get_OrganizationFailed()
        {
            var organization =
                await ParliamentAuthContractStub.GetOrganization.CallAsync(Address.FromString("Test"));
            organization.ShouldBe(new Organization());
        }
        
        [Fact]
        public async Task Get_Proposal()
        {
            _defaultOrganizationAddress = await Get_DefaultOrganizationAddress();
            var proposalId = await Create_Proposal(_defaultOrganizationAddress);
            var getProposal = await ParliamentAuthContractStub.GetProposal.SendAsync(proposalId);
            
            getProposal.Output.Proposer.ShouldBe(DefaultSender);
            getProposal.Output.ContractMethodName.ShouldBe(nameof(TokenContractStub.Transfer));
            getProposal.Output.ProposalId.ShouldBe(proposalId);
            getProposal.Output.OrganizationAddress.ShouldBe(_defaultOrganizationAddress);
            getProposal.Output.ToAddress.ShouldBe(TokenContractAddress);
            getProposal.Output.Params.ShouldBe(_transferInput.ToByteString());
        }
        
        [Fact]
        public async Task Get_ProposalFailed()
        {
            var proposalOutput = await ParliamentAuthContractStub.GetProposal.CallAsync(Hash.FromString("Test"));
            proposalOutput.ShouldBe(new ProposalOutput());
        }

        [Fact]
        public async Task Create_OrganizationFailed()
        {
            _createOrganizationInput =  new CreateOrganizationInput
            {
                ReleaseThreshold = 0
            };
            {
                var transactionResult =
                    await ParliamentAuthContractStub.CreateOrganization.SendAsync(_createOrganizationInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
            }
            {
                _createOrganizationInput.ReleaseThreshold = 100000;
                var transactionResult =
                    await ParliamentAuthContractStub.CreateOrganization.SendAsync(_createOrganizationInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
            }
        }

        [Fact]
        public async Task Create_ProposalFailed()
        {
            _defaultOrganizationAddress = await Get_DefaultOrganizationAddress();
            var blockTime = BlockTimeProvider.GetBlockTime();
            _createProposalInput = new CreateProposalInput
            {
                ToAddress = Address.FromString("Test"),
                Params = ByteString.CopyFromUtf8("Test"),
                ExpiredTime = blockTime.AddDays(1),
                OrganizationAddress =_defaultOrganizationAddress
            };
            //"Invalid proposal."
            //ContractMethodName is null or white space
            {
                var transactionResult = await ParliamentAuthContractStub.CreateProposal.SendAsync(_createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
            }
            //ToAddress is null
            {
                _createProposalInput.ContractMethodName = "Test";
                _createProposalInput.ToAddress = null;
                
                var transactionResult = await ParliamentAuthContractStub.CreateProposal.SendAsync(_createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
            }
            //ExpiredTime is null
            {
                _createProposalInput.ExpiredTime = null;
                _createProposalInput.ToAddress = Address.FromString("Test");
                
                var transactionResult = await ParliamentAuthContractStub.CreateProposal.SendAsync(_createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
            }
            //"Expired proposal."
            {
                _createProposalInput.ExpiredTime = blockTime.AddMilliseconds(5);
                Thread.Sleep(10);
                
                var transactionResult = await ParliamentAuthContractStub.CreateProposal.SendAsync(_createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            }
            //"No registered organization."
            {
                _createProposalInput.ExpiredTime = BlockTimeProvider.GetBlockTime().AddDays(1);
                _createProposalInput.OrganizationAddress = Address.FromString("NoRegisteredOrganizationAddress");
                
                var transactionResult = await ParliamentAuthContractStub.CreateProposal.SendAsync(_createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("No registered organization.").ShouldBeTrue();
            }
            //"Proposal already exists."
            {
                _createProposalInput.OrganizationAddress = _defaultOrganizationAddress;
                var transactionResult1 = await ParliamentAuthContractStub.CreateProposal.SendAsync(_createProposalInput);
                transactionResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
                
                var transactionResult2 = await ParliamentAuthContractStub.CreateProposal.SendAsync(_createProposalInput);
                transactionResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult2.TransactionResult.Error.Contains("Proposal already exists.").ShouldBeTrue();
            }
        }
        
        [Fact]
        public async Task Approve_Proposal_NotFoundProposal()
        {
            var transactionResult = await ParliamentAuthContractStub.Approve.SendAsync(new ApproveInput
            {
                ProposalId = Hash.FromString("Test")
            });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        }

        [Fact]
        public async Task Approve_Proposal_NotAuthorizedApproval()
        {
            _defaultOrganizationAddress = await Get_DefaultOrganizationAddress();
            var proposalId = await Create_Proposal(_defaultOrganizationAddress);
            
            ParliamentAuthContractStub = GetParliamentAuthContractTester(TesterKeyPair);
            var transactionResult = await ParliamentAuthContractStub.Approve.SendAsync(new ApproveInput
            {
                ProposalId = proposalId
            });
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("Not authorized approval.").ShouldBeTrue();
        }
        
        [Fact]
        public async Task Approve_Proposal_ExpiredTime()
        {
            _defaultOrganizationAddress = await Get_DefaultOrganizationAddress();
            var proposalId = await Create_Proposal(_defaultOrganizationAddress);
            
            ParliamentAuthContractStub = GetParliamentAuthContractTester(InitialMinersKeyPairs[0]);
            BlockTimeProvider.SetBlockTime(BlockTimeProvider.GetBlockTime().AddDays(5));
            var transactionResult = await ParliamentAuthContractStub.Approve.CallAsync(new ApproveInput
            {
                ProposalId = proposalId
            });
            transactionResult.Value.ShouldBe(false);
        }

        [Fact]
        public async Task Approve_Proposal_ApprovalAlreadyExists()
        {
            _defaultOrganizationAddress = await Get_DefaultOrganizationAddress();
            var proposalId = await Create_Proposal(_defaultOrganizationAddress);
            
            ParliamentAuthContractStub = GetParliamentAuthContractTester(InitialMinersKeyPairs[0]);            
            var transactionResult1 = await ParliamentAuthContractStub.Approve.SendAsync(new ApproveInput{ProposalId = proposalId});
            transactionResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            transactionResult1.Output.Value.ShouldBe(true);
            
            Thread.Sleep(100);
            var transactionResult2 = await ParliamentAuthContractStub.Approve.SendAsync(new ApproveInput{ProposalId = proposalId});
            transactionResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult2.TransactionResult.Error.Contains("Already approved").ShouldBeTrue();
        }

        [Fact]
        public async Task Approve_And_ReleaseProposal_1()
        {
            _defaultOrganizationAddress = await Get_DefaultOrganizationAddress();
            var proposalId = await Create_Proposal(_defaultOrganizationAddress);
            await TransferForOrganizationAddress(_defaultOrganizationAddress);
            ParliamentAuthContractStub = GetParliamentAuthContractTester(InitialMinersKeyPairs[0]);
            
            var transactionResult1 = await ParliamentAuthContractStub.Approve.SendAsync(new ApproveInput{ProposalId = proposalId});
            transactionResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            transactionResult1.Output.Value.ShouldBe(true);
            
            ParliamentAuthContractStub = GetParliamentAuthContractTester(InitialMinersKeyPairs[1]);
            var transactionResult2 = await ParliamentAuthContractStub.Approve.SendAsync(new ApproveInput{ProposalId = proposalId});
            transactionResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            transactionResult2.Output.Value.ShouldBe(true);
            
//            After release,the proposal will be deleted
//            var getProposal = await ParliamentAuthContractStub.GetProposal.SendAsync(proposalId.Result);
//            getProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
//            getProposal.TransactionResult.Error.Contains("Not found proposal.").ShouldBeTrue();
                      
            var getBalance =TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = "ELF",
                Owner = Tester
            }).Result.Balance;
            getBalance.ShouldBe(100);
        }

        [Fact]
        public async Task Approve_And_ReleaseProposal_2()
        {
            _organizationAddress = await Create_Organization();
            var proposalId = await Create_Proposal(_organizationAddress);
            await TransferForOrganizationAddress(_organizationAddress);
            ParliamentAuthContractStub = GetParliamentAuthContractTester(InitialMinersKeyPairs[0]);
            var transactionResult = await ParliamentAuthContractStub.Approve.SendAsync(new ApproveInput{ProposalId = proposalId});
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            transactionResult.Output.Value.ShouldBe(true);
            
//            After release,the proposal will be deleted
//            var getProposal = await ParliamentAuthContractStub.GetProposal.SendAsync(proposalId.Result);
//            getProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
//            getProposal.TransactionResult.Error.Contains("Not found proposal.").ShouldBeTrue();
            

            var getBalance =TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = "ELF",
                Owner = Tester
            }).Result.Balance;
            getBalance.ShouldBe(100);
        }
        
        [Fact]
        public async Task Approve_And_ReleaseProposalFailed()
        {
            _organizationAddress = await Create_Organization();
            var proposalId = await Create_Proposal(_organizationAddress);
            ParliamentAuthContractStub = GetParliamentAuthContractTester(InitialMinersKeyPairs[0]);
            var transactionResult = await ParliamentAuthContractStub.Approve.SendAsync(new ApproveInput{ProposalId = proposalId});
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            
//            After release,the proposal will be deleted
//            var getProposal = await ParliamentAuthContractStub.GetProposal.SendAsync(proposalId.Result);
//            getProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
//            getProposal.TransactionResult.Error.Contains("Not found proposal.").ShouldBeTrue();
                        
            var getBalance =TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = "ELF",
                Owner = Tester
            }).Result.Balance;
            getBalance.ShouldBe(0);
        }
       
        public async Task<Hash> Create_Proposal(Address organizationAddress)
        {            
            _transferInput = new TransferInput()
            {
                Symbol = "ELF",
                Amount = 100,
                To = Tester,
                Memo = "Transfer"
            };
            _createProposalInput = new CreateProposalInput
            {
                ContractMethodName = nameof(TokenContractStub.Transfer),
                ToAddress = TokenContractAddress,
                Params = _transferInput.ToByteString(),
                ExpiredTime = BlockTimeProvider.GetBlockTime().AddDays(2),
                OrganizationAddress = organizationAddress
            };
            var proposal = await ParliamentAuthContractStub.CreateProposal.SendAsync(_createProposalInput);
            proposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            return proposal.Output;
        }
        
        public async Task<Address> Create_Organization()
        {           
            _createOrganizationInput =  new CreateOrganizationInput
            {
                ReleaseThreshold = 10000 / MinersCount
            };
            var transactionResult =
                await ParliamentAuthContractStub.CreateOrganization.SendAsync(_createOrganizationInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            return transactionResult.Output;
        }

        public async Task<Address> Get_DefaultOrganizationAddress()
        {
             _defaultOrganizationAddress =
                await ParliamentAuthContractStub.GetDefaultOrganizationAddress.CallAsync(new Empty());

            return _defaultOrganizationAddress;
        }
        
        public async Task TransferForOrganizationAddress(Address to)
        {
            await TokenContractStub.Transfer.SendAsync(new TransferInput
            {
                Symbol = "ELF",
                Amount = 200,
                To = to,
                Memo = "transfer organization address"
            });
        }
    }
}