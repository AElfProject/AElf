using System.Linq;
using System.Threading.Tasks;
using Acs0;
using Acs3;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Contracts.Deployer;
using AElf.Contracts.ParliamentAuth;
using AElf.Kernel;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Genesis
{
    public class GenesisContractAuthTest : BasicContractZeroTestBase
    {
        [Fact]
        public async Task Initialize_AlreadyExist_Test()
        {
            var txResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(ACS0Container.ACS0Stub.ChangeGenesisOwner), SampleAddress.AddressList[0]);

            txResult.Status.ShouldBe(TransactionResultStatus.Failed);
            txResult.Error.Contains("Unauthorized behavior.").ShouldBeTrue();
        }

        [Fact]
        public async Task DeploySmartContracts_Test()
        {
            var contractDeploymentInput = new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("TokenConverter")).Value)
            };

            var releaseApprovedContractInput = await ProposeContractAsync(
                nameof(BasicContractZeroContainer.BasicContractZeroStub.ProposeNewContract), contractDeploymentInput,
                TesterKeyPair);

            var approveResult0 =
                await ApproveWithKeyPairAsync(releaseApprovedContractInput.ProposalId, Tester.InitialMinerList[0]);
            approveResult0.Status.ShouldBe(TransactionResultStatus.Mined);

            var approveResult1 =
                await ApproveWithKeyPairAsync(releaseApprovedContractInput.ProposalId, Tester.InitialMinerList[1]);
            approveResult1.Status.ShouldBe(TransactionResultStatus.Mined);

            var deploymentResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZeroContainer.BasicContractZeroStub.ReleaseApprovedContract),
                releaseApprovedContractInput);

            deploymentResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var creator = ContractDeployed.Parser.ParseFrom(deploymentResult.Logs[1].Indexed[0]).Creator;
            creator.ShouldBe(Tester.GetCallOwnerAddress());
            var deployAddress = ContractDeployed.Parser.ParseFrom(deploymentResult.Logs[1].NonIndexed).Address;
            deployAddress.ShouldNotBeNull();
        }

        [Fact]
        public async Task UpdateSmartContract_Test()
        {
            var contractDeploymentInput = new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("TokenConverter")).Value)
            };

            var newAddress = await DeployAsync(contractDeploymentInput, TesterKeyPair);

            var code = Codes.Single(kv => kv.Key.Contains("Treasury")).Value;
            var contractUpdateInput = new ContractUpdateInput
            {
                Address = newAddress,
                Code = ByteString.CopyFrom(code)
            };

            var releaseApprovedContractInput = await ProposeContractAsync(
                nameof(BasicContractZeroContainer.BasicContractZeroStub.ProposeUpdateContract), contractUpdateInput,
                TesterKeyPair);
            var approveResult0 =
                await ApproveWithKeyPairAsync(releaseApprovedContractInput.ProposalId, Tester.InitialMinerList[0]);
            approveResult0.Status.ShouldBe(TransactionResultStatus.Mined);

            var approveResult1 =
                await ApproveWithKeyPairAsync(releaseApprovedContractInput.ProposalId, Tester.InitialMinerList[1]);
            approveResult1.Status.ShouldBe(TransactionResultStatus.Mined);

            var updateResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZeroContainer.BasicContractZeroStub.ReleaseApprovedContract),
                releaseApprovedContractInput);

            var contractAddress = CodeUpdated.Parser.ParseFrom(updateResult.Logs[1].Indexed[0]).Address;
            contractAddress.ShouldBe(newAddress);
            var codeHash = Hash.FromRawBytes(code);
            var newHash = CodeUpdated.Parser.ParseFrom(updateResult.Logs[1].NonIndexed).NewCodeHash;
            newHash.ShouldBe(codeHash);
        }

        [Fact]
        public async Task DeploySmartContractWithCodeCheck_Test()
        {
            var proposalId = await CreateProposalAsync(
                nameof(BasicContractZeroContainer.BasicContractZeroStub.ProposeNewContract), new ContractDeploymentInput
                {
                    Category = KernelConstants.DefaultRunnerCategory,
                    Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("MultiToken")).Value)
                }, TesterKeyPair);
            await ApproveWithMinersAsync(proposalId);
            var result = await ReleaseProposalAsync(proposalId, TesterKeyPair);
            result.Status.ShouldBe(TransactionResultStatus.Mined);

            // Transaction logs should contain below two events
            result.Logs.Select(l => l.Name).ShouldContain(nameof(CodeCheckRequired));
            result.Logs.Select(l => l.Name).ShouldContain(nameof(ProposalCreated));

            // Wait for contract code check event handler to finish its job
            // Mine a block, should include approval transaction
            var block = await Tester.MineEmptyBlockAsync();

            var txs = await Tester.GetTransactionsAsync(block.TransactionIds);

            txs.First(tx => tx.To == ParliamentAddress).MethodName
                .ShouldBe(nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.ApproveMultiProposals));
        }

        [Fact]
        public async Task UpdateSmartContractWithCodeCheck_Test()
        {
            var contractDeploymentInput = new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("TokenConverter")).Value)
            };

            var newAddress = await DeployAsync(contractDeploymentInput, TesterKeyPair);

            var code = Codes.Single(kv => kv.Key.Contains("ReferendumAuth")).Value;
            var contractUpdateInput = new ContractUpdateInput
            {
                Address = newAddress,
                Code = ByteString.CopyFrom(code)
            };

            var proposalId = await CreateProposalAsync(
                nameof(BasicContractZeroContainer.BasicContractZeroStub.ProposeUpdateContract), contractUpdateInput,
                TesterKeyPair);
            await ApproveWithMinersAsync(proposalId);
            var result = await ReleaseProposalAsync(proposalId, TesterKeyPair);

            result.Status.ShouldBe(TransactionResultStatus.Mined);
            // Transaction logs should contain below two events
            result.Logs.Select(l => l.Name).ShouldContain(nameof(CodeCheckRequired));
            result.Logs.Select(l => l.Name).ShouldContain(nameof(ProposalCreated));

            // Wait for contract code check event handler to finish its job
            // Mine a block, should include approval transaction
            var block = await Tester.MineEmptyBlockAsync();

            var txs = await Tester.GetTransactionsAsync(block.TransactionIds);

            txs.First(tx => tx.To == ParliamentAddress).MethodName
                .ShouldBe(nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.ApproveMultiProposals));
        }
        
        [Fact]
        public async Task ChangeContractAuthor_OrganizationToOrganization_Test()
        {
            var contractDeploymentInput = new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("TokenConverter")).Value)
            };

            var releaseApprovedContractInput = await ProposeContractAsync(
                nameof(BasicContractZeroContainer.BasicContractZeroStub.ProposeNewContract), contractDeploymentInput,
                TesterKeyPair);

            var approveResult0 =
                await ApproveWithKeyPairAsync(releaseApprovedContractInput.ProposalId, Tester.InitialMinerList[0]);
            approveResult0.Status.ShouldBe(TransactionResultStatus.Mined);

            var approveResult1 =
                await ApproveWithKeyPairAsync(releaseApprovedContractInput.ProposalId, Tester.InitialMinerList[1]);
            approveResult1.Status.ShouldBe(TransactionResultStatus.Mined);

            var deploymentResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZeroContainer.BasicContractZeroStub.ReleaseApprovedContract),
                releaseApprovedContractInput);

            deploymentResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var creator = ContractDeployed.Parser.ParseFrom(deploymentResult.Logs[1].Indexed[0]).Creator;
            creator.ShouldBe(Tester.GetCallOwnerAddress());
            var deployAddress = ContractDeployed.Parser.ParseFrom(deploymentResult.Logs[1].NonIndexed).Address;

            var changeAuthor = Address.FromPublicKey(Tester.InitialMinerList.Last().PublicKey);
            var input = new ChangeContractAuthorInput
            {
                ContractAddress = deployAddress,
                NewAuthor = changeAuthor
            };
            var changeProposalId = await CreateProposalAsync(nameof(BasicContractZeroContainer.BasicContractZeroStub.ChangeContractAuthor), input,TesterKeyPair);
            await ApproveWithMinersAsync(changeProposalId);
            var changeResult = await ReleaseProposalAsync(changeProposalId, TesterKeyPair);
            changeResult.Status.ShouldBe(TransactionResultStatus.Failed);
            changeResult.Error.Contains("Failed to change contract author.").ShouldBeTrue();
        }
        
        [Fact]
        public async Task ChangeContractAuthor_OrganizationToAccount_Test()
        {
            var contractDeploymentInput = new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("TokenConverter")).Value)
            };

            var releaseApprovedContractInput = await ProposeContractAsync(
                nameof(BasicContractZeroContainer.BasicContractZeroStub.ProposeNewContract), contractDeploymentInput,
                TesterKeyPair);

            var approveResult0 =
                await ApproveWithKeyPairAsync(releaseApprovedContractInput.ProposalId, Tester.InitialMinerList[0]);
            approveResult0.Status.ShouldBe(TransactionResultStatus.Mined);

            var approveResult1 =
                await ApproveWithKeyPairAsync(releaseApprovedContractInput.ProposalId, Tester.InitialMinerList[1]);
            approveResult1.Status.ShouldBe(TransactionResultStatus.Mined);

            var deploymentResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZeroContainer.BasicContractZeroStub.ReleaseApprovedContract),
                releaseApprovedContractInput);

            deploymentResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var creator = ContractDeployed.Parser.ParseFrom(deploymentResult.Logs[1].Indexed[0]).Creator;
            creator.ShouldBe(Tester.GetCallOwnerAddress());
            var deployAddress = ContractDeployed.Parser.ParseFrom(deploymentResult.Logs[1].NonIndexed).Address;

            var changeAuthor = Address.FromPublicKey(Tester.InitialMinerList.Last().PublicKey);
            var input = new ChangeContractAuthorInput
            {
                ContractAddress = deployAddress,
                NewAuthor = changeAuthor
            };
            var changeProposalId = await CreateProposalAsync(nameof(BasicContractZeroContainer.BasicContractZeroStub.ChangeContractAuthor), input,TesterKeyPair);
            await ApproveWithMinersAsync(changeProposalId);
            var changeResult = await ReleaseProposalAsync(changeProposalId, TesterKeyPair);
            changeResult.Status.ShouldBe(TransactionResultStatus.Failed);
            changeResult.Error.Contains("Failed to change contract author.").ShouldBeTrue();
        }
        
        [Fact]
        public async Task ChangeSystemContractAuthor_Test()
        {
            var deployAddress = TokenContractAddress;
            var changeAuthor = Address.FromPublicKey(Tester.InitialMinerList.Last().PublicKey);
            var input = new ChangeContractAuthorInput
            {
                ContractAddress = deployAddress,
                NewAuthor = changeAuthor
            };
            var changeProposalId = await CreateProposalAsync(nameof(BasicContractZeroContainer.BasicContractZeroStub.ChangeContractAuthor), input,TesterKeyPair);
            await ApproveWithMinersAsync(changeProposalId);
            var changeResult = await ReleaseProposalAsync(changeProposalId, TesterKeyPair);
            changeResult.Status.ShouldBe(TransactionResultStatus.Failed);
            changeResult.Error.Contains("Failed to change contract author.").ShouldBeTrue();
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

            const string proposingMethodName = nameof(BasicContractZero.ProposeUpdateContract);
            var proposalId = await CreateProposalAsync(proposingMethodName, contractUpdateInput, TesterKeyPair);
            var txResult1 = await ApproveWithMinersAsync(proposalId);
            txResult1.Status.ShouldBe(TransactionResultStatus.Mined);
            var releaseResult = await ReleaseProposalAsync(proposalId, TesterKeyPair);
            releaseResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var proposedContractInputHash = CodeCheckRequired.Parser
                .ParseFrom(releaseResult.Logs.First(l => l.Name.Contains(nameof(CodeCheckRequired))).NonIndexed)
                .ProposedContractInputHash;
            var codeCheckProposalId = ProposalCreated.Parser
                .ParseFrom(releaseResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
                .ProposalId;

            var approveResult0 = await ApproveWithKeyPairAsync(codeCheckProposalId, Tester.InitialMinerList[0]);
            approveResult0.Status.ShouldBe(TransactionResultStatus.Mined);

            var approveResult1 = await ApproveWithKeyPairAsync(codeCheckProposalId, Tester.InitialMinerList[1]);
            approveResult1.Status.ShouldBe(TransactionResultStatus.Mined);

            var result = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZeroContainer.BasicContractZeroStub.ReleaseApprovedContract),
                new ReleaseApprovedContractInput
                    {ProposedContractInputHash = proposedContractInputHash, ProposalId = codeCheckProposalId});
            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task ChangeContractZeroOwner_Test_Invalid_Address()
        {
            var address = Tester.GetCallOwnerAddress();
            var methodName = "ChangeGenesisOwner";
            var proposalId = await CreateProposalAsync(methodName, address, TesterKeyPair);
            var txResult1 = await ApproveWithMinersAsync(proposalId);
            txResult1.Status.ShouldBe(TransactionResultStatus.Mined);
            var txResult2 = await ReleaseProposalAsync(proposalId, TesterKeyPair);
            txResult2.Status.ShouldBe(TransactionResultStatus.Failed);
        }

        [Fact]
        public async Task ChangeContractZeroOwner_Test()
        {
            var createOrganizationResult = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
                nameof(ParliamentAuthContractContainer.ParliamentAuthContractStub.CreateOrganization),
                new CreateOrganizationInput
                {
                    ReleaseThreshold = 1000
                });

            var organizationAddress = Address.Parser.ParseFrom(createOrganizationResult.ReturnValue);

            var methodName = "ChangeGenesisOwner";
            var proposalId = await CreateProposalAsync(methodName, organizationAddress, TesterKeyPair);
            var txResult1 = await ApproveWithMinersAsync(proposalId);
            txResult1.Status.ShouldBe(TransactionResultStatus.Mined);
            var txResult2 = await ReleaseProposalAsync(proposalId, TesterKeyPair);
            txResult2.Status.ShouldBe(TransactionResultStatus.Mined);

            // test deployment with only one miner
            var contractDeploymentInput = new ContractDeploymentInput()
            {
                Category = KernelConstants.DefaultRunnerCategory,
                Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("TokenConverter")).Value)
            };
            var releaseApprovedContractInput = await ProposeContractAsync(
                nameof(BasicContractZeroContainer.BasicContractZeroStub.ProposeNewContract), contractDeploymentInput,
                TesterKeyPair, organizationAddress);
            await ApproveWithKeyPairAsync(releaseApprovedContractInput.ProposalId, Tester.InitialMinerList[0]);

            var deploymentResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZeroContainer.BasicContractZeroStub.ReleaseApprovedContract),
                releaseApprovedContractInput);

            var deployAddress = ContractDeployed.Parser.ParseFrom(deploymentResult.Logs[1].NonIndexed).Address;
            deployAddress.ShouldNotBeNull();
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

            var proposalId = await CreateProposalAsync(
                nameof(BasicContractZeroContainer.BasicContractZeroStub.ProposeNewContract), contractDeploymentInput,
                AnotherUserKeyPair);
            await ApproveWithMinersAsync(proposalId);
            var releaseResult = await ReleaseProposalAsync(proposalId, AnotherUserKeyPair);
            releaseResult.Status.ShouldBe(TransactionResultStatus.Failed);
            releaseResult.Error.Contains("Proposer authority validation failed.").ShouldBeTrue();
        }

        [Fact]
        public async Task DeploySmartContracts_RepeatedProposals_Test()
        {
            var contractDeploymentInput = new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("TokenConverter")).Value)
            };
            var proposalId = await CreateProposalAsync(
                nameof(BasicContractZeroContainer.BasicContractZeroStub.ProposeNewContract), contractDeploymentInput,
                TesterKeyPair);
            await ApproveWithMinersAsync(proposalId);
            await ReleaseProposalAsync(proposalId, TesterKeyPair);
            var proposalId2 = await CreateProposalAsync(
                nameof(BasicContractZeroContainer.BasicContractZeroStub.ProposeNewContract), contractDeploymentInput,
                TesterKeyPair);
            await ApproveWithMinersAsync(proposalId2);
            var releaseResult = await ReleaseProposalAsync(proposalId2, TesterKeyPair);
            releaseResult.Status.ShouldBe(TransactionResultStatus.Failed);
            releaseResult.Error.Contains("Already proposed.").ShouldBeTrue();
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
                nameof(ACS0Container.ACS0Stub.ChangeGenesisOwner), Tester.GetCallOwnerAddress());
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
    }
}