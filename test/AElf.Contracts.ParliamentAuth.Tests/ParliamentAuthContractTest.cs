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
        public ParliamentAuthContractTest()
        {
            InitializeContracts();
        }

        [Fact]
        public async Task Get_DefaultOrganizationAddressFailed()
        {
            var transactionResult =
                await OtherParliamentAuthContractStub.GetGenesisOwnerAddress.SendAsync(new Empty());
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("Not initialized.").ShouldBeTrue();
        }

        [Fact]
        public async Task ParliamentAuthContract_Initialize()
        {
            var result = await ParliamentAuthContractStub.Initialize.SendAsync(
                new InitializeInput {GenesisOwnerReleaseThreshold = 6666});
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task ParliamentAuthContract_InitializeTwice()
        {
            await ParliamentAuthContract_Initialize();

            var result = await ParliamentAuthContractStub.Initialize.SendAsync(
                new InitializeInput {GenesisOwnerReleaseThreshold = 6666});
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.Contains("Already initialized.").ShouldBeTrue();
        }


        [Fact]
        public async Task Get_Organization()
        {
            var createOrganizationInput = new CreateOrganizationInput
            {
                ReleaseThreshold = 10000 / MinersCount
            };
            var transactionResult =
                await ParliamentAuthContractStub.CreateOrganization.SendAsync(createOrganizationInput);
            var organizationAddress = transactionResult.Output;
            var getOrganization = await ParliamentAuthContractStub.GetOrganization.CallAsync(organizationAddress);


            getOrganization.OrganizationAddress.ShouldBe(organizationAddress);
            getOrganization.ReleaseThreshold.ShouldBe(10000 / MinersCount);
            getOrganization.OrganizationHash.ShouldBe(Hash.FromTwoHashes(
                Hash.FromMessage(ParliamentAuthContractAddress), Hash.FromMessage(createOrganizationInput)));
        }

        [Fact]
        public async Task Get_OrganizationFailed()
        {
            var transactionResult =
                await ParliamentAuthContractStub.GetOrganization.SendAsync(Address.FromString("Test"));
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
                To = Tester,
                Memo = "Transfer"
            };
            var proposalId = await CreateProposalAsync(organizationAddress);
            var getProposal = await ParliamentAuthContractStub.GetProposal.SendAsync(proposalId);

            getProposal.Output.Proposer.ShouldBe(DefaultSender);
            getProposal.Output.ContractMethodName.ShouldBe(nameof(TokenContractStub.Transfer));
            getProposal.Output.ProposalId.ShouldBe(proposalId);
            getProposal.Output.OrganizationAddress.ShouldBe(organizationAddress);
            getProposal.Output.ToAddress.ShouldBe(TokenContractAddress);
            getProposal.Output.Params.ShouldBe(transferInput.ToByteString());
        }

        [Fact]
        public async Task Get_ProposalFailed()
        {
            var transactionResult = await ParliamentAuthContractStub.GetProposal.SendAsync(Hash.FromString("Test"));
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.TransactionResult.Error.Contains("Not found proposal.").ShouldBeTrue();
        }

        [Fact]
        public async Task Create_OrganizationFailed()
        {
            var createOrganizationInput = new CreateOrganizationInput
            {
                ReleaseThreshold = 0
            };
            {
                var transactionResult =
                    await ParliamentAuthContractStub.CreateOrganization.SendAsync(createOrganizationInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
            }
            {
                createOrganizationInput.ReleaseThreshold = 100000;
                var transactionResult =
                    await ParliamentAuthContractStub.CreateOrganization.SendAsync(createOrganizationInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
            }
        }

        [Fact]
        public async Task Create_ProposalFailed()
        {
            var organizationAddress = await CreateOrganizationAsync();
            var blockTime = BlockTimeProvider.GetBlockTime();
            var createProposalInput = new CreateProposalInput
            {
                ToAddress = Address.FromString("Test"),
                Params = ByteString.CopyFromUtf8("Test"),
                ExpiredTime = blockTime.AddDays(1),
                OrganizationAddress = organizationAddress
            };
            //"Invalid proposal."
            //ContractMethodName is null or white space
            {
                var transactionResult = await ParliamentAuthContractStub.CreateProposal.SendAsync(createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
            }
            //ToAddress is null
            {
                createProposalInput.ContractMethodName = "Test";
                createProposalInput.ToAddress = null;

                var transactionResult = await ParliamentAuthContractStub.CreateProposal.SendAsync(createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
            }
            //ExpiredTime is null
            {
                createProposalInput.ExpiredTime = null;
                createProposalInput.ToAddress = Address.FromString("Test");

                var transactionResult = await ParliamentAuthContractStub.CreateProposal.SendAsync(createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
            }
            //"Expired proposal."
            {
                createProposalInput.ExpiredTime = blockTime.AddMilliseconds(5);
                Thread.Sleep(10);

                var transactionResult = await ParliamentAuthContractStub.CreateProposal.SendAsync(createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("Expired proposal.").ShouldBeTrue();
            }
            //"No registered organization."
            {
                createProposalInput.ExpiredTime = BlockTimeProvider.GetBlockTime().AddDays(1);
                createProposalInput.OrganizationAddress = Address.FromString("NoRegisteredOrganizationAddress");

                var transactionResult = await ParliamentAuthContractStub.CreateProposal.SendAsync(createProposalInput);
                transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.TransactionResult.Error.Contains("No registered organization.").ShouldBeTrue();
            }
            //"Proposal already exists."
            {
                createProposalInput.OrganizationAddress = organizationAddress;
                var transactionResult1 = await ParliamentAuthContractStub.CreateProposal.SendAsync(createProposalInput);
                transactionResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

                var transactionResult2 = await ParliamentAuthContractStub.CreateProposal.SendAsync(createProposalInput);
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
            transactionResult.TransactionResult.Error.Contains("Not found proposal.").ShouldBeTrue();
        }

        [Fact]
        public async Task Approve_Proposal_NotAuthorizedApproval()
        {
            var organizationAddress = await CreateOrganizationAsync();
            var proposalId = await CreateProposalAsync(organizationAddress);

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
            var organizationAddress = await CreateOrganizationAsync();
            var proposalId = await CreateProposalAsync(organizationAddress);

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
            var organizationAddress = await CreateOrganizationAsync();
            var proposalId = await CreateProposalAsync(organizationAddress);

            ParliamentAuthContractStub = GetParliamentAuthContractTester(InitialMinersKeyPairs[0]);
            var transactionResult1 =
                await ParliamentAuthContractStub.Approve.SendAsync(new ApproveInput {ProposalId = proposalId});
            transactionResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            transactionResult1.Output.Value.ShouldBe(true);

            Thread.Sleep(100);
            var transactionResult2 =
                await ParliamentAuthContractStub.Approve.SendAsync(new ApproveInput {ProposalId = proposalId});
            transactionResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult2.TransactionResult.Error.Contains("Approval already existed.").ShouldBeTrue();
        }

        [Fact]
        public async Task Approve_And_ReleaseProposal_1()
        {
            var organizationAddress = await CreateOrganizationAsync();
            var proposalId = await CreateProposalAsync(organizationAddress);
            await TransferForOrganizationAddressAsync(organizationAddress);
            ParliamentAuthContractStub = GetParliamentAuthContractTester(InitialMinersKeyPairs[0]);

            var transactionResult1 =
                await ParliamentAuthContractStub.Approve.SendAsync(new ApproveInput {ProposalId = proposalId});
            transactionResult1.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            transactionResult1.Output.Value.ShouldBe(true);

            ParliamentAuthContractStub = GetParliamentAuthContractTester(InitialMinersKeyPairs[1]);
            var transactionResult2 =
                await ParliamentAuthContractStub.Approve.SendAsync(new ApproveInput {ProposalId = proposalId});
            transactionResult2.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            transactionResult2.Output.Value.ShouldBe(true);

//            After release,the proposal will be deleted
//            var getProposal = await ParliamentAuthContractStub.GetProposal.SendAsync(proposalId.Result);
//            getProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
//            getProposal.TransactionResult.Error.Contains("Not found proposal.").ShouldBeTrue();

            var getBalance = TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = "ELF",
                Owner = Tester
            }).Result.Balance;
            getBalance.ShouldBe(100);
        }

        [Fact]
        public async Task Approve_And_ReleaseProposal_2()
        {
            var createOrganizationInput = new CreateOrganizationInput
            {
                ReleaseThreshold = 10000 / MinersCount
            };
            var transactionResult =
                await ParliamentAuthContractStub.CreateOrganization.SendAsync(createOrganizationInput);
            var organizationAddress = transactionResult.Output;

            var proposalId = await CreateProposalAsync(organizationAddress);
            await TransferForOrganizationAddressAsync(organizationAddress);

            ParliamentAuthContractStub = GetParliamentAuthContractTester(InitialMinersKeyPairs[0]);
            var txResult =
                await ParliamentAuthContractStub.Approve.SendAsync(new ApproveInput {ProposalId = proposalId});
            txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            txResult.Output.Value.ShouldBe(true);

//            After release,the proposal will be deleted
//            var getProposal = await ParliamentAuthContractStub.GetProposal.SendAsync(proposalId.Result);
//            getProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
//            getProposal.TransactionResult.Error.Contains("Not found proposal.").ShouldBeTrue();


            var getBalance = TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = "ELF",
                Owner = Tester
            }).Result.Balance;
            getBalance.ShouldBe(100);
        }

        [Fact]
        public async Task Approve_And_ReleaseProposalFailed()
        {
            var createOrganizationInput = new CreateOrganizationInput
            {
                ReleaseThreshold = 10000 / MinersCount
            };
            var transactionResult =
                await ParliamentAuthContractStub.CreateOrganization.SendAsync(createOrganizationInput);
            var organizationAddress = transactionResult.Output;
            var proposalId = await CreateProposalAsync(organizationAddress);
            ParliamentAuthContractStub = GetParliamentAuthContractTester(InitialMinersKeyPairs[0]);
            var txResult =
                await ParliamentAuthContractStub.Approve.SendAsync(new ApproveInput {ProposalId = proposalId});
            txResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);

//            After release,the proposal will be deleted
//            var getProposal = await ParliamentAuthContractStub.GetProposal.SendAsync(proposalId.Result);
//            getProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
//            getProposal.TransactionResult.Error.Contains("Not found proposal.").ShouldBeTrue();

            var getBalance = TokenContractStub.GetBalance.CallAsync(new GetBalanceInput
            {
                Symbol = "ELF",
                Owner = Tester
            }).Result.Balance;
            getBalance.ShouldBe(0);
        }

        private async Task<Hash> CreateProposalAsync(Address organizationAddress)
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
            var proposal = await ParliamentAuthContractStub.CreateProposal.SendAsync(createProposalInput);
            proposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            return proposal.Output;
        }

        private async Task<Address> CreateOrganizationAsync()
        {
            var createOrganizationInput = new CreateOrganizationInput
            {
                ReleaseThreshold = 20000 / MinersCount
            };
            var transactionResult =
                await ParliamentAuthContractStub.CreateOrganization.SendAsync(createOrganizationInput);
            transactionResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            return transactionResult.Output;
        }

        private async Task TransferForOrganizationAddressAsync(Address to)
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