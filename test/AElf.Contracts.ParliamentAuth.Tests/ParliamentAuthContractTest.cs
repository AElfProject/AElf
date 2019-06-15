using System.Threading;
using System.Threading.Tasks;
using Acs3;
using AElf.Contracts.MultiToken.Messages;
using AElf.Kernel;
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
        [Fact]
        public async Task ParliamentAuthContract_InitializeMultiTimes()
        {
            var transactionResult =
                (await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                    nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Initialize), new InitializeInput
                        {GenesisOwnerReleaseThreshold = 6666}));
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Already initialized.").ShouldBeTrue();
        }

        [Fact]
        public async Task Get_Organization()
        {
            var createOrganizationInput = new CreateOrganizationInput
            {
                ReleaseThreshold = 10000 / Tester.InitialMinerList.Count
            };

            var organizationAddress = await CreateOrganizationAsync();
            var transactionResult = await Tester.CallContractMethodAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.GetOrganization),
                organizationAddress);
            var getOrganization = Organization.Parser.ParseFrom(transactionResult);

            getOrganization.OrganizationAddress.ShouldBe(organizationAddress);
            getOrganization.ReleaseThreshold.ShouldBe(10000 / Tester.InitialMinerList.Count);
            getOrganization.OrganizationHash.ShouldBe(Hash.FromTwoHashes(
                Hash.FromMessage(ParliamentAddress), Hash.FromMessage(createOrganizationInput)));
        }

        [Fact]
        public async Task Get_OrganizationFailed()
        {
            var transactionResult =
                await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                    nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.GetOrganization),
                    Address.FromString("Test"));
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("No registered organization.").ShouldBeTrue();
        }

        [Fact]
        public async Task Get_Proposal()
        {
            var transferInput = new TransferInput()
            {
                Symbol = "ELF",
                Amount = 100,
                To = otherTester.GetCallOwnerAddress(),
                Memo = "Transfer"
            };

            var defaultOrganizationAddress = await GetDefaultOrganizationAddressAsync();
            var proposalId = await CreateProposalAsync(defaultOrganizationAddress);
            var transactionResult = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.GetProposal), proposalId);
            var getProposal = ProposalOutput.Parser.ParseFrom(transactionResult.ReturnValue);

            getProposal.Proposer.ShouldBe(Tester.GetCallOwnerAddress());
            getProposal.ContractMethodName.ShouldBe(nameof(TokenContractContainer.TokenContractStub.Transfer));
            getProposal.ProposalId.ShouldBe(proposalId);
            getProposal.OrganizationAddress.ShouldBe(defaultOrganizationAddress);
            getProposal.ToAddress.ShouldBe(TokenContractAddress);
            getProposal.Params.ShouldBe(transferInput.ToByteString());
        }

        [Fact]
        public async Task Get_ProposalFailed()
        {
            var transactionResult = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.GetProposal),
                Hash.FromString("Test"));
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Not found proposal.").ShouldBeTrue();
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
                    await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                        nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.CreateOrganization),
                        createOrganizationInput);
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
            }
            {
                createOrganizationInput.ReleaseThreshold = 100000;
                var transactionResult =
                    await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                        nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.CreateOrganization),
                        createOrganizationInput);
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Invalid organization.").ShouldBeTrue();
            }
        }

        [Fact]
        public async Task Create_ProposalFailed()
        {
            var defaultOrganizationAddress = await GetDefaultOrganizationAddressAsync();

            var createProposalInput = new CreateProposalInput
            {
                ToAddress = Address.FromString("Test"),
                Params = ByteString.CopyFromUtf8("Test"),
                ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                OrganizationAddress = defaultOrganizationAddress
            };
            //"Invalid proposal."
            //ContractMethodName is null or white space
            {
                var transactionResult = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                    nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.CreateProposal),
                    createProposalInput);
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
            }
            //ToAddress is null
            {
                createProposalInput.ContractMethodName = "Test";
                createProposalInput.ToAddress = null;

                var transactionResult = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                    nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.CreateProposal),
                    createProposalInput);
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
            }
            //ExpiredTime is null
            {
                createProposalInput.ExpiredTime = null;
                createProposalInput.ToAddress = Address.FromString("Test");

                var transactionResult = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                    nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.CreateProposal),
                    createProposalInput);
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Invalid proposal.").ShouldBeTrue();
            }
            //"Expired proposal."
            {
                createProposalInput.ExpiredTime = TimestampHelper.GetUtcNow();
                Thread.Sleep(100);

                var transactionResult = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                    nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.CreateProposal),
                    createProposalInput);
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("Expired proposal.").ShouldBeTrue();
            }
            //"No registered organization."
            {
                createProposalInput.ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1);
                createProposalInput.OrganizationAddress = Address.FromString("NoRegisteredOrganizationAddress");

                var transactionResult = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                    nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.CreateProposal),
                    createProposalInput);
                transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult.Error.Contains("No registered organization.").ShouldBeTrue();
            }
            //"Proposal already exists."
            {
                createProposalInput.OrganizationAddress = defaultOrganizationAddress;
                var transactionResult1 = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                    nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.CreateProposal),
                    createProposalInput);
                transactionResult1.Status.ShouldBe(TransactionResultStatus.Mined);

                var transactionResult2 = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                    nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.CreateProposal),
                    createProposalInput);
                transactionResult2.Status.ShouldBe(TransactionResultStatus.Failed);
                transactionResult2.Error.Contains("Proposal already exists.").ShouldBeTrue();
            }
        }

        [Fact]
        public async Task Approve_Proposal_NotFoundProposal()
        {
            var transactionResult = await minerTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Approve), new ApproveInput
                {
                    ProposalId = Hash.FromString("Test")
                });
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Not found proposal.").ShouldBeTrue();
        }

        [Fact]
        public async Task Approve_Proposal_NotAuthorizedApproval()
        {
            var defaultOrganizationAddress = await GetDefaultOrganizationAddressAsync();
            var proposalId = await CreateProposalAsync(defaultOrganizationAddress);

            var transactionResult = await otherTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Approve), new ApproveInput
                {
                    ProposalId = proposalId
                });
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult.Error.Contains("Not authorized approval.").ShouldBeTrue();
        }

        [Fact]
        public async Task Approve_Proposal_ExpiredTime()
        {
            var transferInput = new TransferInput()
            {
                Symbol = "ELF",
                Amount = 100,
                To = otherTester.GetCallOwnerAddress(),
                Memo = "Transfer"
            };

            var organizationAddress = await CreateOrganizationAsync();
            var proposal = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.CreateProposal),
                new CreateProposalInput
                {
                    ContractMethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
                    ExpiredTime = TimestampHelper.GetUtcNow().AddMilliseconds(100),
                    Params = transferInput.ToByteString(),
                    ToAddress = TokenContractAddress,
                    OrganizationAddress = organizationAddress
                });
            var proposalId = Hash.Parser.ParseFrom(proposal.ReturnValue);

            Thread.Sleep(500);
            var transactionResult = await minerTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Approve), new ApproveInput
                {
                    ProposalId = proposalId
                });

            transactionResult.ReadableReturnValue.ShouldBe("false");
        }

        [Fact]
        public async Task Approve_Proposal_ApprovalAlreadyExists()
        {
            var defaultOrganizationAddress = await GetDefaultOrganizationAddressAsync();
            var proposalId = await CreateProposalAsync(defaultOrganizationAddress);

            var transactionResult1 = await minerTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Approve), new ApproveInput
                {
                    ProposalId = proposalId
                });
            transactionResult1.Status.ShouldBe(TransactionResultStatus.Mined);
            transactionResult1.ReadableReturnValue.ShouldBe("true");

            Thread.Sleep(100);
            var transactionResult2 = await minerTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Approve), new ApproveInput
                {
                    ProposalId = proposalId
                });
            transactionResult2.Status.ShouldBe(TransactionResultStatus.Failed);
            transactionResult2.Error.Contains("Approval already existed.").ShouldBeTrue();
        }

        [Fact]
        public async Task Approve_And_ReleaseProposal_1()
        {
            var defaultOrganizationAddress = await GetDefaultOrganizationAddressAsync();
            var proposalId = await CreateProposalAsync(defaultOrganizationAddress);
            await TransferForOrganizationAddressAsync(defaultOrganizationAddress);

            var transactionResult1 = await minerTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Approve), new ApproveInput
                {
                    ProposalId = proposalId
                });
            transactionResult1.Status.ShouldBe(TransactionResultStatus.Mined);
            transactionResult1.ReadableReturnValue.ShouldBe("true");

            var minerTester2 = Tester.CreateNewContractTester(Tester.InitialMinerList[1]);
            var transactionResult2 = await minerTester2.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Approve), new ApproveInput
                {
                    ProposalId = proposalId
                });
            transactionResult2.Status.ShouldBe(TransactionResultStatus.Mined);
            transactionResult2.ReadableReturnValue.ShouldBe("true");

//            After release,the proposal will be deleted
//            var getProposal = await ParliamentAuthContractStub.GetProposal.SendAsync(proposalId.Result);
//            getProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
//            getProposal.TransactionResult.Error.Contains("Not found proposal.").ShouldBeTrue();

            var transactionResult = Tester.CallContractMethodAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.GetBalance), new GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = otherTester.GetCallOwnerAddress(),
                });
            GetBalanceOutput.Parser.ParseFrom(transactionResult.Result).Balance.ShouldBe(100);
        }

        [Fact]
        public async Task Approve_And_ReleaseProposal_2()
        {
            var organizationAddress = await CreateOrganizationAsync();
            var proposalId = await CreateProposalAsync(organizationAddress);
            await TransferForOrganizationAddressAsync(organizationAddress);

            var transactionResult = await minerTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Approve), new ApproveInput
                {
                    ProposalId = proposalId
                });
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            transactionResult.ReadableReturnValue.ShouldBe("true");

//            After release,the proposal will be deleted
//            var getProposal = await ParliamentAuthContractStub.GetProposal.SendAsync(proposalId.Result);
//            getProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
//            getProposal.TransactionResult.Error.Contains("Not found proposal.").ShouldBeTrue();

            var getBalanceResult = Tester.CallContractMethodAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.GetBalance), new GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = otherTester.GetCallOwnerAddress(),
                });
            GetBalanceOutput.Parser.ParseFrom(getBalanceResult.Result).Balance.ShouldBe(100);
        }

        [Fact]
        public async Task Approve_And_ReleaseProposalFailed()
        {
            var organizationAddress = await CreateOrganizationAsync();
            var proposalId = await CreateProposalAsync(organizationAddress);
            var transactionResult = await minerTester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.Approve),
                new ApproveInput {ProposalId = proposalId});
            transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);

//            After release,the proposal will be deleted
//            var getProposal = await ParliamentAuthContractStub.GetProposal.SendAsync(proposalId.Result);
//            getProposal.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
//            getProposal.TransactionResult.Error.Contains("Not found proposal.").ShouldBeTrue();

            var getBalanceResult = Tester.CallContractMethodAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.GetBalance), new GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = otherTester.GetCallOwnerAddress()
                });
            GetBalanceOutput.Parser.ParseFrom(getBalanceResult.Result).Balance.ShouldBe(0);
        }

        private async Task<Hash> CreateProposalAsync(Address organizationAddress)
        {
            var transferInput = new TransferInput()
            {
                Symbol = "ELF",
                Amount = 100,
                To = otherTester.GetCallOwnerAddress(),
                Memo = "Transfer"
            };

            var proposal = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.CreateProposal),
                new CreateProposalInput
                {
                    ContractMethodName = nameof(TokenContractContainer.TokenContractStub.Transfer),
                    ExpiredTime = TimestampHelper.GetUtcNow().AddDays(1),
                    Params = transferInput.ToByteString(),
                    ToAddress = TokenContractAddress,
                    OrganizationAddress = organizationAddress
                });
            var proposalId = Hash.Parser.ParseFrom(proposal.ReturnValue);
            return proposalId;
        }

        private async Task<Address> CreateOrganizationAsync()
        {
            var createOrganizationInput = new CreateOrganizationInput
            {
                ReleaseThreshold = 10000 / Tester.InitialMinerList.Count
            };
            var transactionResult =
                await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                    nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.CreateOrganization),
                    createOrganizationInput);
            transactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var organizationAddress = Address.Parser.ParseFrom(transactionResult.ReturnValue);
            return organizationAddress;
        }

        private async Task<Address> GetDefaultOrganizationAddressAsync()
        {
            var transactionResult =
                await Tester.CallContractMethodAsync(ParliamentAddress,
                    nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.GetGenesisOwnerAddress),
                    new Empty());
            var defaultOrganizationAddress = Address.Parser.ParseFrom(transactionResult);
            return defaultOrganizationAddress;
        }

        private async Task TransferForOrganizationAddressAsync(Address to)
        {
            await Tester.ExecuteContractWithMiningAsync(TokenContractAddress,
                nameof(TokenContractContainer.TokenContractStub.Transfer), new TransferInput
                {
                    Symbol = "ELF",
                    Amount = 200,
                    To = to,
                    Memo = "transfer organization address"
                });
        }
    }
}