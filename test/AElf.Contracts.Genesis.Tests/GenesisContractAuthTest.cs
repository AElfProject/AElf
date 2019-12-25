using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Acs0;
using Acs3;
using AElf.Contracts.Parliament;
using AElf.Kernel;
using AElf.Kernel.Token;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Genesis
{
    public class GenesisContractAuthTest : BasicContractZeroTestBase
    {
        #region Main Chain 

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
                proposingTxResult.Error.Contains("Proposer authority validation failed.").ShouldBeTrue();
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
            var methodName = "ChangeGenesisOwner";
            var proposalId = await CreateProposalAsync(Tester, ParliamentAddress, methodName, address);
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
            var methodName = "ChangeGenesisOwner";
            var proposalId = await CreateProposalAsync(Tester, ParliamentAddress, methodName, organizationAddress);
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

            await ApproveWithKeyPairAsync(Tester, ParliamentAddress, contractProposalId,
                Tester.InitialMinerList.First());

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

            await ApproveWithKeyPairAsync(Tester, ParliamentAddress, codeCheckProposalId,
                Tester.InitialMinerList.First());

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

            var proposalId = await CreateProposalAsync(otherTester, ParliamentAddress,
                nameof(BasicContractZeroContainer.BasicContractZeroStub.ProposeNewContract), contractDeploymentInput);

            await ApproveWithMinersAsync(Tester, ParliamentAddress, proposalId);
            var releaseResult = await ReleaseProposalAsync(otherTester, ParliamentAddress, proposalId);
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

            var proposalId = await CreateProposalAsync(SideChainMiner, SideParliamentAddress,
                nameof(BasicContractZeroContainer.BasicContractZeroStub.ProposeNewContract), contractDeploymentInput);

            await ApproveWithMinersAsync(SideChainTester, SideParliamentAddress, proposalId);
            var releaseResult = await ReleaseProposalAsync(SideChainMiner, SideParliamentAddress, proposalId);
            releaseResult.Status.ShouldBe(TransactionResultStatus.Failed);
            releaseResult.Error.Contains("Proposer authority validation failed.").ShouldBeTrue();
        }

        #endregion
    }
}