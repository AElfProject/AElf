using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Acs0;
using Acs3;
using AElf.Contracts.Association;
using AElf.Contracts.Parliament;
using AElf.Kernel;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;
using CreateOrganizationInput = AElf.Contracts.Parliament.CreateOrganizationInput;
using ProposalCreated = Acs3.ProposalCreated;

namespace AElf.Contracts.Genesis
{
    public class GenesisContractAuthTest : BasicContractZeroTestBase
    {
        #region Main Chain

        [Fact]
        public async Task Initialize_AlreadyExist_Test()
        {
            var txResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZeroContainer.BasicContractZeroStub.ChangeContractDeploymentController),
                new AuthorityStuff()
                {
                    OwnerAddress = SampleAddress.AddressList[0],
                    ContractAddress = BasicContractZeroAddress
                });

            txResult.Status.ShouldBe(TransactionResultStatus.Failed);
            txResult.Error.ShouldContain("Unauthorized behavior.");
        }

        [Fact]
        public async Task DeploySmartContracts_Test()
        {
            var contractDeploymentInput = new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("TokenConverter")).Value)
            };

            // propose contract code
            var proposingTxResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZero.ProposeNewContract), contractDeploymentInput);
            proposingTxResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var proposalId = ProposalCreated.Parser
                .ParseFrom(proposingTxResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
                .ProposalId;
            proposalId.ShouldNotBeNull();
            var proposedContractInputHash = ContractProposed.Parser
                .ParseFrom(proposingTxResult.Logs.First(l => l.Name.Contains(nameof(ContractProposed))).NonIndexed)
                .ProposedContractInputHash;
            await ApproveWithMinersAsync(Tester, ParliamentAddress, proposalId);

            // release contract code and trigger code check proposal
            var releaseApprovedContractTxResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZero.ReleaseApprovedContract), new ReleaseContractInput
                {
                    ProposalId = proposalId,
                    ProposedContractInputHash = proposedContractInputHash
                });
            releaseApprovedContractTxResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var codeCheckProposalId = ProposalCreated.Parser
                .ParseFrom(releaseApprovedContractTxResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                    .NonIndexed).ProposalId;
            codeCheckProposalId.ShouldNotBeNull();

            await ApproveWithMinersAsync(Tester, ParliamentAddress, codeCheckProposalId);

            // release code check proposal and deployment completes
            var deploymentResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZeroContainer.BasicContractZeroStub.ReleaseCodeCheckedContract),
                new ReleaseContractInput
                    {ProposedContractInputHash = proposedContractInputHash, ProposalId = codeCheckProposalId});
            deploymentResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var creator = ContractDeployed.Parser.ParseFrom(deploymentResult.Logs[1].Indexed[0]).Author;
            creator.ShouldBe(BasicContractZeroAddress);
            var deployAddress = ContractDeployed.Parser.ParseFrom(deploymentResult.Logs[1].NonIndexed).Address;
            deployAddress.ShouldNotBeNull();

            var author = Address.Parser.ParseFrom(await Tester.CallContractMethodAsync(BasicContractZeroAddress,
                nameof(BasicContractZeroContainer.BasicContractZeroStub.GetContractAuthor), deployAddress));
            author.ShouldBe(BasicContractZeroAddress);
        }

        [Fact]
        public async Task Deploy_MultiTimes()
        {
            var contractDeploymentInput = new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("TokenConverter")).Value)
            };

            {
                var address = await DeployAsync(Tester, ParliamentAddress, contractDeploymentInput);
                address.ShouldNotBeNull();
            }

            {
                var address = await DeployAsync(Tester, ParliamentAddress, contractDeploymentInput);
                address.ShouldNotBeNull();
            }

            {
                var minerTester = Tester.CreateNewContractTester(AnotherMinerKeyPair);
                var address = await DeployAsync(minerTester, ParliamentAddress, contractDeploymentInput);
                address.ShouldNotBeNull();
            }
            {
                var otherTester = Tester.CreateNewContractTester(AnotherUserKeyPair);
                var proposingTxResult = await otherTester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                    nameof(BasicContractZero.ProposeNewContract), contractDeploymentInput);
                proposingTxResult.Status.ShouldBe(TransactionResultStatus.Failed);
                proposingTxResult.Error.ShouldContain("Unauthorized to propose.");
            }
        }

        [Fact]
        public async Task UpdateSmartContract_Test()
        {
            var contractDeploymentInput = new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("TokenConverter")).Value)
            };

            var newAddress = await DeployAsync(Tester, ParliamentAddress, contractDeploymentInput);
            var code = Codes.Single(kv => kv.Key.Contains("Treasury")).Value;
            var contractUpdateInput = new ContractUpdateInput
            {
                Address = newAddress,
                Code = ByteString.CopyFrom(code)
            };

            var proposingTxResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZero.ProposeUpdateContract), contractUpdateInput);
            proposingTxResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var proposalId = ProposalCreated.Parser
                .ParseFrom(proposingTxResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
                .ProposalId;
            proposalId.ShouldNotBeNull();
            var proposedContractInputHash = ContractProposed.Parser
                .ParseFrom(proposingTxResult.Logs.First(l => l.Name.Contains(nameof(ContractProposed))).NonIndexed)
                .ProposedContractInputHash;
            await ApproveWithMinersAsync(Tester, ParliamentAddress, proposalId);

            var releaseApprovedContractTxResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZero.ReleaseApprovedContract), new ReleaseContractInput
                {
                    ProposalId = proposalId,
                    ProposedContractInputHash = proposedContractInputHash
                });
            releaseApprovedContractTxResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var codeCheckProposalId = ProposalCreated.Parser
                .ParseFrom(releaseApprovedContractTxResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                    .NonIndexed).ProposalId;
            codeCheckProposalId.ShouldNotBeNull();

            await ApproveWithMinersAsync(Tester, ParliamentAddress, codeCheckProposalId);

            var updateResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZeroContainer.BasicContractZeroStub.ReleaseCodeCheckedContract),
                new ReleaseContractInput
                    {ProposedContractInputHash = proposedContractInputHash, ProposalId = codeCheckProposalId});
            updateResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var contractAddress = CodeUpdated.Parser
                .ParseFrom(updateResult.Logs.First(l => l.Name.Contains(nameof(CodeUpdated))).Indexed[0]).Address;
            contractAddress.ShouldBe(newAddress);
            var codeHash = Hash.FromRawBytes(code);
            var newHash = CodeUpdated.Parser
                .ParseFrom(updateResult.Logs.First(l => l.Name.Contains(nameof(CodeUpdated))).NonIndexed).NewCodeHash;
            newHash.ShouldBe(codeHash);
        }

        [Fact(Skip = "Skip due to need task delay.")]
        public async Task DeploySmartContractWithCodeCheck_Test()
        {
            var contractCode = ReadCode(Path.Combine(BaseDir, "AElf.Contracts.MultiToken.dll"));
            var contractDeploymentInput = new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory,
                Code = ByteString.CopyFrom(contractCode)
            };
            // propose contract code
            var proposingTxResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZero.ProposeNewContract), contractDeploymentInput);

            var proposalId = ProposalCreated.Parser
                .ParseFrom(proposingTxResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
                .ProposalId;
            var proposedContractInputHash = ContractProposed.Parser
                .ParseFrom(proposingTxResult.Logs.First(l => l.Name.Contains(nameof(ContractProposed))).NonIndexed)
                .ProposedContractInputHash;
            await ApproveWithMinersAsync(Tester, ParliamentAddress, proposalId);

            // release contract code and trigger code check proposal
            await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZero.ReleaseApprovedContract), new ReleaseContractInput
                {
                    ProposalId = proposalId,
                    ProposedContractInputHash = proposedContractInputHash
                });

            // Wait for contract code check event handler to finish its job
            // Mine a block, should include approval transaction
            var block = await Tester.MineEmptyBlockAsync();
            var txs = await Tester.GetTransactionsAsync(block.TransactionIds);
            var parliamentTxs = txs.Where(tx => tx.To == ParliamentAddress).ToList();
            parliamentTxs[0].MethodName
                .ShouldBe(nameof(ParliamentContractContainer.ParliamentContractStub.ApproveMultiProposals));
        }

        [Fact(Skip = "Skip due to need task delay.")]
        public async Task UpdateSmartContractWithCodeCheck_Test()
        {
            var contractCode = ReadCode(Path.Combine(BaseDir, "AElf.Contracts.TokenConverter.dll"));
            var contractDeploymentInput = new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = ByteString.CopyFrom(contractCode)
            };

            var newAddress = await DeployAsync(Tester, ParliamentAddress, contractDeploymentInput);

            var code = ReadCode(Path.Combine(BaseDir, "AElf.Contracts.Referendum.dll"));
            var contractUpdateInput = new ContractUpdateInput
            {
                Address = newAddress,
                Code = ByteString.CopyFrom(code)
            };

            var proposingTxResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZero.ProposeUpdateContract), contractUpdateInput);
            proposingTxResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var proposalId = ProposalCreated.Parser
                .ParseFrom(proposingTxResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
                .ProposalId;
            proposalId.ShouldNotBeNull();
            var proposedContractInputHash = ContractProposed.Parser
                .ParseFrom(proposingTxResult.Logs.First(l => l.Name.Contains(nameof(ContractProposed))).NonIndexed)
                .ProposedContractInputHash;
            await ApproveWithMinersAsync(Tester, ParliamentAddress, proposalId);

            var releaseApprovedContractTxResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZero.ReleaseApprovedContract), new ReleaseContractInput
                {
                    ProposalId = proposalId,
                    ProposedContractInputHash = proposedContractInputHash
                });
            releaseApprovedContractTxResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var codeCheckProposalId = ProposalCreated.Parser
                .ParseFrom(releaseApprovedContractTxResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                    .NonIndexed).ProposalId;
            codeCheckProposalId.ShouldNotBeNull();

            // Wait for contract code check event handler to finish its job
            // Mine a block, should include approval transaction
            var block = await Tester.MineEmptyBlockAsync();
            var txs = await Tester.GetTransactionsAsync(block.TransactionIds);
            var parliamentTxs = txs.Where(tx => tx.To == ParliamentAddress).ToList();
            parliamentTxs[0].MethodName
                .ShouldBe(nameof(ParliamentContractContainer.ParliamentContractStub.ApproveMultiProposals));
        }

        [Fact]
        public async Task Update_ZeroContract_Test()
        {
            var code = Codes.Single(kv => kv.Key.Contains("GenesisUpdate")).Value;

            var contractUpdateInput = new ContractUpdateInput
            {
                Address = BasicContractZeroAddress,
                Code = ByteString.CopyFrom(code)
            };

            var proposingTxResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZero.ProposeUpdateContract), contractUpdateInput);

            proposingTxResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var proposalId = ProposalCreated.Parser
                .ParseFrom(proposingTxResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
                .ProposalId;

            proposalId.ShouldNotBeNull();

            var proposedContractInputHash = ContractProposed.Parser
                .ParseFrom(proposingTxResult.Logs.First(l => l.Name.Contains(nameof(ContractProposed))).NonIndexed)
                .ProposedContractInputHash;

            await ApproveWithMinersAsync(Tester, ParliamentAddress, proposalId);

            var releaseApprovedContractTxResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZero.ReleaseApprovedContract), new ReleaseContractInput
                {
                    ProposalId = proposalId,
                    ProposedContractInputHash = proposedContractInputHash
                });

            releaseApprovedContractTxResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var codeCheckProposalId = ProposalCreated.Parser
                .ParseFrom(releaseApprovedContractTxResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                    .NonIndexed).ProposalId;

            codeCheckProposalId.ShouldNotBeNull();
            await ApproveWithMinersAsync(Tester, ParliamentAddress, codeCheckProposalId);

            var result = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZeroContainer.BasicContractZeroStub.ReleaseCodeCheckedContract),
                new ReleaseContractInput
                    {ProposedContractInputHash = proposedContractInputHash, ProposalId = codeCheckProposalId});

            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task ChangeContractZeroOwner_Test_Invalid_Address()
        {
            var address = Tester.GetCallOwnerAddress();
            var contractDeploymentController = await GetContractDeploymentController(Tester, BasicContractZeroAddress);
            const string proposalCreationMethodName =
                nameof(BasicContractZeroContainer.BasicContractZeroStub.ChangeContractDeploymentController);
            var proposalId = await CreateProposalAsync(Tester, contractDeploymentController.ContractAddress,
                contractDeploymentController.OwnerAddress, proposalCreationMethodName,
                new AuthorityStuff
                {
                    ContractAddress = contractDeploymentController.ContractAddress,
                    OwnerAddress = address
                });
            await ApproveWithMinersAsync(Tester, ParliamentAddress, proposalId);
            var txResult2 = await ReleaseProposalAsync(Tester, ParliamentAddress, proposalId);
            txResult2.Status.ShouldBe(TransactionResultStatus.Failed);
        }

        [Fact]
        public async Task ChangeContractZeroOwner_Test()
        {
            var createOrganizationResult = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentContractContainer.ParliamentContractStub.CreateOrganization),
                new CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MinimalApprovalThreshold = 1000,
                        MinimalVoteThreshold = 1000
                    }
                });

            var organizationAddress = Address.Parser.ParseFrom(createOrganizationResult.ReturnValue);

            var contractDeploymentController = await GetContractDeploymentController(Tester, BasicContractZeroAddress);
            const string proposalCreationMethodName =
                nameof(BasicContractZeroContainer.BasicContractZeroStub.ChangeContractDeploymentController);
            var proposalId = await CreateProposalAsync(Tester, contractDeploymentController.ContractAddress,
                contractDeploymentController.OwnerAddress, proposalCreationMethodName,
                new AuthorityStuff
                {
                    OwnerAddress = organizationAddress,
                    ContractAddress = ParliamentAddress
                });
            await ApproveWithMinersAsync(Tester, ParliamentAddress, proposalId);
            var txResult2 = await ReleaseProposalAsync(Tester, ParliamentAddress, proposalId);
            txResult2.Status.ShouldBe(TransactionResultStatus.Mined);

            // test deployment with only one miner
            var contractDeploymentInput = new ContractDeploymentInput()
            {
                Category = KernelConstants.DefaultRunnerCategory,
                Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("TokenConverter")).Value)
            };

            // propose contract code
            var proposingTxResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZero.ProposeNewContract), contractDeploymentInput);

            var contractProposalId = ProposalCreated.Parser
                .ParseFrom(proposingTxResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
                .ProposalId;

            var proposedContractInputHash = ContractProposed.Parser
                .ParseFrom(proposingTxResult.Logs.First(l => l.Name.Contains(nameof(ContractProposed))).NonIndexed)
                .ProposedContractInputHash;

            var tester = Tester.CreateNewContractTester(Tester.InitialMinerList.First());
            await ApproveWithTesterAsync(tester, ParliamentAddress, contractProposalId);

            // release contract code and trigger code check proposal
            var releaseApprovedContractTxResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZero.ReleaseApprovedContract), new ReleaseContractInput
                {
                    ProposalId = contractProposalId,
                    ProposedContractInputHash = proposedContractInputHash
                });

            var codeCheckProposalId = ProposalCreated.Parser
                .ParseFrom(releaseApprovedContractTxResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                    .NonIndexed).ProposalId;

            await ApproveWithMinersAsync(Tester, ParliamentAddress, codeCheckProposalId);

            // release code check proposal and deployment completes
            var deploymentResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZeroContainer.BasicContractZeroStub.ReleaseCodeCheckedContract),
                new ReleaseContractInput
                    {ProposedContractInputHash = proposedContractInputHash, ProposalId = codeCheckProposalId});

            var creator = ContractDeployed.Parser
                .ParseFrom(deploymentResult.Logs.First(l => l.Name.Contains(nameof(ContractDeployed))).Indexed[0])
                .Author;

            creator.ShouldBe(BasicContractZeroAddress);

            var deployAddress = ContractDeployed.Parser
                .ParseFrom(deploymentResult.Logs.First(l => l.Name.Contains(nameof(ContractDeployed))).NonIndexed)
                .Address;

            deployAddress.ShouldNotBeNull();

            var author = Address.Parser.ParseFrom(await Tester.CallContractMethodAsync(BasicContractZeroAddress,
                nameof(BasicContractZeroContainer.BasicContractZeroStub.GetContractAuthor), deployAddress));

            author.ShouldBe(BasicContractZeroAddress);
        }

        [Fact]
        public async Task ChangeContractZeroOwnerByAssociation_Test()
        {
            var createOrganizationResult = await Tester.ExecuteContractWithMiningAsync(AssociationContractAddress,
                nameof(AssociationContractContainer.AssociationContractStub.CreateOrganization),
                new Association.CreateOrganizationInput
                {
                    ProposalReleaseThreshold = new ProposalReleaseThreshold
                    {
                        MinimalApprovalThreshold = 1,
                        MinimalVoteThreshold = 1
                    },
                    ProposerWhiteList = new ProposerWhiteList
                    {
                        Proposers = {AnotherMinerAddress}
                    },
                    OrganizationMemberList = new OrganizationMemberList
                    {
                        OrganizationMembers = {AnotherMinerAddress}
                    }
                });

            var organizationAddress = Address.Parser.ParseFrom(createOrganizationResult.ReturnValue);

            var contractDeploymentController = await GetContractDeploymentController(Tester, BasicContractZeroAddress);
            const string proposalCreationMethodName =
                nameof(BasicContractZeroContainer.BasicContractZeroStub.ChangeContractDeploymentController);
            var proposalId = await CreateProposalAsync(Tester, contractDeploymentController.ContractAddress,
                contractDeploymentController.OwnerAddress, proposalCreationMethodName,
                new AuthorityStuff
                {
                    OwnerAddress = organizationAddress,
                    ContractAddress = AssociationContractAddress
                });
            await ApproveWithMinersAsync(Tester, ParliamentAddress, proposalId);
            var txResult2 = await ReleaseProposalAsync(Tester, ParliamentAddress, proposalId);
            txResult2.Status.ShouldBe(TransactionResultStatus.Mined);

            var contractDeploymentControllerAfterChanged =
                await GetContractDeploymentController(Tester, BasicContractZeroAddress);

            contractDeploymentControllerAfterChanged.ContractAddress.ShouldBe(AssociationContractAddress);
            contractDeploymentControllerAfterChanged.OwnerAddress.ShouldBe(organizationAddress);

            // test deployment with only one miner
            var contractDeploymentInput = new ContractDeploymentInput()
            {
                Category = KernelConstants.DefaultRunnerCategory,
                Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("TokenConverter")).Value)
            };

            var anotherMinerTester = Tester.CreateNewContractTester(AnotherMinerKeyPair);

            // propose contract code
            var proposingTxResult = await anotherMinerTester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZero.ProposeNewContract), contractDeploymentInput);

            var contractProposalId = ProposalCreated.Parser
                .ParseFrom(proposingTxResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
                .ProposalId;

            var proposedContractInputHash = ContractProposed.Parser
                .ParseFrom(proposingTxResult.Logs.First(l => l.Name.Contains(nameof(ContractProposed))).NonIndexed)
                .ProposedContractInputHash;

            await ApproveWithTesterAsync(anotherMinerTester, AssociationContractAddress, contractProposalId);

            // release contract code and trigger code check proposal
            var releaseApprovedContractTxResult = await anotherMinerTester.ExecuteContractWithMiningAsync(
                BasicContractZeroAddress,
                nameof(BasicContractZero.ReleaseApprovedContract), new ReleaseContractInput
                {
                    ProposalId = contractProposalId,
                    ProposedContractInputHash = proposedContractInputHash
                });

            var codeCheckProposalId = ProposalCreated.Parser
                .ParseFrom(releaseApprovedContractTxResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                    .NonIndexed).ProposalId;

            await ApproveWithMinersAsync(Tester, ParliamentAddress, codeCheckProposalId);

            // release code check proposal and deployment completes
            var deploymentResult = await anotherMinerTester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZeroContainer.BasicContractZeroStub.ReleaseCodeCheckedContract),
                new ReleaseContractInput
                    {ProposedContractInputHash = proposedContractInputHash, ProposalId = codeCheckProposalId});

            var creator = ContractDeployed.Parser
                .ParseFrom(deploymentResult.Logs.First(l => l.Name.Contains(nameof(ContractDeployed))).Indexed[0])
                .Author;

            creator.ShouldBe(AnotherMinerAddress);

            var deployAddress = ContractDeployed.Parser
                .ParseFrom(deploymentResult.Logs.First(l => l.Name.Contains(nameof(ContractDeployed))).NonIndexed)
                .Address;
            var author = Address.Parser.ParseFrom(await Tester.CallContractMethodAsync(BasicContractZeroAddress,
                nameof(BasicContractZeroContainer.BasicContractZeroStub.GetContractAuthor), deployAddress));

            author.ShouldBe(AnotherMinerAddress);
        }

        [Fact]
        public async Task DeploySmartContracts_WithoutAuth_Test()
        {
            var txResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(ACS0Container.ACS0Stub.DeploySmartContract), (new ContractDeploymentInput()
                {
                    Category = KernelConstants.DefaultRunnerCategory,
                    Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("MultiToken")).Value)
                }));

            txResult.Status.ShouldBe(TransactionResultStatus.Failed);
            txResult.Error.Contains("Unauthorized behavior.").ShouldBeTrue();
        }

        [Fact]
        public async Task DeploySmartContracts_WithWrongProposer_Test()
        {
            var contractDeploymentInput = new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("TokenConverter")).Value)
            };

            var otherTester = Tester.CreateNewContractTester(AnotherUserKeyPair);
            var result = await otherTester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(ACS0Container.ACS0Stub.ProposeNewContract), contractDeploymentInput);
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.ShouldContain("Unauthorized to propose.");
        }

        [Fact]
        public async Task DeploySmartContracts_RepeatedProposals_Test()
        {
            var contractDeploymentInput = new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("TokenConverter")).Value)
            };

            // propose contract code
            var proposingTxResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZero.ProposeNewContract), contractDeploymentInput);

            {
                // propose contract code
                var repeatedProposingTxResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                    nameof(BasicContractZero.ProposeNewContract), contractDeploymentInput);
                repeatedProposingTxResult.Status.ShouldBe(TransactionResultStatus.Failed);
                repeatedProposingTxResult.Error.Contains("Already proposed.").ShouldBeTrue();
            }

            var proposalId = ProposalCreated.Parser
                .ParseFrom(proposingTxResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
                .ProposalId;

            var proposedContractInputHash = ContractProposed.Parser
                .ParseFrom(proposingTxResult.Logs.First(l => l.Name.Contains(nameof(ContractProposed))).NonIndexed)
                .ProposedContractInputHash;

            await ApproveWithMinersAsync(Tester, ParliamentAddress, proposalId);

            // release contract code and trigger code check proposal
            var releaseApprovedContractTxResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZero.ReleaseApprovedContract), new ReleaseContractInput
                {
                    ProposalId = proposalId,
                    ProposedContractInputHash = proposedContractInputHash
                });

            var codeCheckProposalId = ProposalCreated.Parser
                .ParseFrom(releaseApprovedContractTxResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                    .NonIndexed).ProposalId;

            await ApproveWithMinersAsync(Tester, ParliamentAddress, codeCheckProposalId);
            {
                // propose contract code
                var repeatedProposingTxResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                    nameof(BasicContractZero.ProposeNewContract), contractDeploymentInput);
                repeatedProposingTxResult.Status.ShouldBe(TransactionResultStatus.Failed);
                repeatedProposingTxResult.Error.Contains("Already proposed.").ShouldBeTrue();
            }

            // release code check proposal and deployment completes
            var deploymentResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZeroContainer.BasicContractZeroStub.ReleaseCodeCheckedContract),
                new ReleaseContractInput
                    {ProposedContractInputHash = proposedContractInputHash, ProposalId = codeCheckProposalId});

            deploymentResult.Status.ShouldBe(TransactionResultStatus.Mined);
            {
                // propose contract code
                var repeatedProposingTxResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                    nameof(BasicContractZero.ProposeNewContract), contractDeploymentInput);
                repeatedProposingTxResult.Status.ShouldBe(TransactionResultStatus.Mined);
            }
        }

        [Fact]
        public async Task UpdateSmartContract_WithoutAuth_Test()
        {
            var result = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(ACS0Container.ACS0Stub.UpdateSmartContract), (
                    new ContractUpdateInput()
                    {
                        Address = ParliamentAddress,
                        Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("Consensus")).Value)
                    }));

            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Unauthorized behavior.").ShouldBeTrue();
        }

        [Fact]
        public async Task ChangeContractZeroOwner_WithoutAuth_Test()
        {
            var result = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZeroContainer.BasicContractZeroStub.ChangeContractDeploymentController),
                new AuthorityStuff()
                {
                    OwnerAddress = Tester.GetCallOwnerAddress(),
                    ContractAddress = BasicContractZeroAddress
                });

            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Unauthorized behavior.").ShouldBeTrue();
        }

        [Fact]
        public async Task ValidateSystemContractAddress_Test()
        {
            var result = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(ACS0Container.ACS0Stub.ValidateSystemContractAddress), new ValidateSystemContractAddressInput
                {
                    Address = TokenContractAddress,
                    SystemContractHashName = TokenSmartContractAddressNameProvider.Name
                });

            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task ValidateSystemContractAddress_WrongAddress_Test()
        {
            var result = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(ACS0Container.ACS0Stub.ValidateSystemContractAddress), new ValidateSystemContractAddressInput
                {
                    Address = ParliamentAddress,
                    SystemContractHashName = TokenSmartContractAddressNameProvider.Name
                });

            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Address not expected.").ShouldBeTrue();
        }

        #endregion

        #region Side chain

        [Fact]
        public async Task DeploySmartContracts_CreatorDeploy_Test()
        {
            StartSideChain();

            var contractDeploymentInput = new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("TokenConverter")).Value)
            };

            // propose contract code
            var proposingTxResult = await SideChainTester.ExecuteContractWithMiningAsync(SideBasicContractZeroAddress,
                nameof(BasicContractZero.ProposeNewContract), contractDeploymentInput);

            proposingTxResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var proposalId = ProposalCreated.Parser
                .ParseFrom(proposingTxResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
                .ProposalId;

            proposalId.ShouldNotBeNull();

            var proposedContractInputHash = ContractProposed.Parser
                .ParseFrom(proposingTxResult.Logs.First(l => l.Name.Contains(nameof(ContractProposed))).NonIndexed)
                .ProposedContractInputHash;

            await ApproveWithMinersAsync(SideChainTester, SideParliamentAddress, proposalId);

            // release contract code and trigger code check proposal
            var releaseApprovedContractTxResult = await SideChainTester.ExecuteContractWithMiningAsync(
                SideBasicContractZeroAddress,
                nameof(BasicContractZero.ReleaseApprovedContract), new ReleaseContractInput
                {
                    ProposalId = proposalId,
                    ProposedContractInputHash = proposedContractInputHash
                });

            releaseApprovedContractTxResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var codeCheckProposalId = ProposalCreated.Parser
                .ParseFrom(releaseApprovedContractTxResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated)))
                    .NonIndexed).ProposalId;

            codeCheckProposalId.ShouldNotBeNull();
            await ApproveWithMinersAsync(SideChainTester, SideParliamentAddress, codeCheckProposalId);

            // release code check proposal and deployment completes
            var deploymentResult = await SideChainTester.ExecuteContractWithMiningAsync(SideBasicContractZeroAddress,
                nameof(BasicContractZeroContainer.BasicContractZeroStub.ReleaseCodeCheckedContract),
                new ReleaseContractInput
                    {ProposedContractInputHash = proposedContractInputHash, ProposalId = codeCheckProposalId});

            deploymentResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var creator = ContractDeployed.Parser.ParseFrom(deploymentResult.Logs[1].Indexed[0]).Author;
            creator.ShouldBe(SideChainTester.GetCallOwnerAddress());
            var deployAddress = ContractDeployed.Parser.ParseFrom(deploymentResult.Logs[1].NonIndexed).Address;
            deployAddress.ShouldNotBeNull();

            var author = Address.Parser.ParseFrom(await SideChainTester.CallContractMethodAsync(
                SideBasicContractZeroAddress,
                nameof(BasicContractZeroContainer.BasicContractZeroStub.GetContractAuthor), deployAddress));

            author.ShouldBe(SideChainTester.GetCallOwnerAddress());
        }

        [Fact]
        public async Task DeploySmartContracts_MinerDeploy_Test()
        {
            StartSideChain();

            var contractDeploymentInput = new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("TokenConverter")).Value)
            };

            var result = await SideChainMinerTester.ExecuteContractWithMiningAsync(SideBasicContractZeroAddress,
                nameof(ACS0Container.ACS0Stub.ProposeNewContract), contractDeploymentInput);
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.ShouldContain("Unauthorized to propose.");
        }

        #endregion
    }
}