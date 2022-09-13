using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Association;
using AElf.Contracts.Parliament;
using AElf.CSharp.Core.Extension;
using AElf.Kernel;
using AElf.Kernel.CodeCheck.Infrastructure;
using AElf.Kernel.Token;
using AElf.Standards.ACS0;
using AElf.Standards.ACS3;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;
using CreateOrganizationInput = AElf.Contracts.Parliament.CreateOrganizationInput;

namespace AElf.Contracts.Genesis;

public class GenesisContractAuthTest : BasicContractZeroTestBase
{
    [Fact]
    public async Task SetInitialController_Failed_Test()
    {
        var txResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.SetInitialControllerAddress),
            ParliamentAddress);
        txResult.Status.ShouldBe(TransactionResultStatus.Failed);
        txResult.Error.ShouldContain("Genesis owner already initialized");
    }

    [Fact]
    public async Task SetContractProposerRequiredState_Failed_Test()
    {
        var txResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.SetContractProposerRequiredState),
            ParliamentAddress);
        txResult.Status.ShouldBe(TransactionResultStatus.Failed);
        txResult.Error.ShouldContain("Genesis contract already initialized");
    }

    #region Main Chain

    [Fact]
    public async Task Initialize_AlreadyExist_Test()
    {
        {
            var txResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.Initialize), new InitializeInput
                {
                    ContractDeploymentAuthorityRequired = false
                });

            txResult.Status.ShouldBe(TransactionResultStatus.Failed);
            txResult.Error.ShouldContain("Contract zero already initialized.");
        }

        {
            var txResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.ChangeContractDeploymentController),
                new AuthorityInfo
                {
                    OwnerAddress = SampleAddress.AddressList[0],
                    ContractAddress = BasicContractZeroAddress
                });

            txResult.Status.ShouldBe(TransactionResultStatus.Failed);
            txResult.Error.ShouldContain("Unauthorized behavior.");
        }
    }

    [Fact]
    public async Task DeploySystemContract_Test()
    {
        var txResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.DeploySystemSmartContract),
            new SystemContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("MultiToken")).Value)
            });

        txResult.Status.ShouldBe(TransactionResultStatus.Failed);
        txResult.Error.ShouldContain("System contract deployment failed.");
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

        {
            var noPermissionCodeCheckProposingTxResult = await Tester.ExecuteContractWithMiningAsync(
                BasicContractZeroAddress,
                nameof(BasicContractZero.ProposeContractCodeCheck), new ContractCodeCheckInput
                {
                    ProposedContractInputHash = proposedContractInputHash
                });
            noPermissionCodeCheckProposingTxResult.Status.ShouldBe(TransactionResultStatus.Failed);
            noPermissionCodeCheckProposingTxResult.Error.ShouldContain("Unauthorized behavior.");
        }

        {
            // not proposed
            var releaseNotExistApprovedContractTxResult = await Tester.ExecuteContractWithMiningAsync(
                BasicContractZeroAddress,
                nameof(BasicContractZero.ReleaseApprovedContract), new ReleaseContractInput
                {
                    ProposalId = proposalId,
                    ProposedContractInputHash = HashHelper.ComputeFrom("Random")
                });
            releaseNotExistApprovedContractTxResult.Status.ShouldBe(TransactionResultStatus.Failed);
            releaseNotExistApprovedContractTxResult.Error.ShouldContain("Invalid contract proposing status.");
        }

        {
            // wrong sender
            var releaseApprovedContractWithWrongSenderTx = await Tester.GenerateTransactionAsync(
                BasicContractZeroAddress,
                nameof(BasicContractZero.ReleaseApprovedContract), AnotherMinerKeyPair, new ReleaseContractInput
                {
                    ProposalId = proposalId,
                    ProposedContractInputHash = proposedContractInputHash
                });
            var blockReturnSet = await Tester.MineAsync(new List<Transaction>
                { releaseApprovedContractWithWrongSenderTx });
            var noPermissionProposingTxResult =
                blockReturnSet.TransactionResultMap[releaseApprovedContractWithWrongSenderTx.GetHash()];
            noPermissionProposingTxResult.Status.ShouldBe(TransactionResultStatus.Failed);
            noPermissionProposingTxResult.Error.ShouldContain("Invalid contract proposing status.");
        }

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

        {
            var releaseAlreadyApprovedContractTxResult = await Tester.ExecuteContractWithMiningAsync(
                BasicContractZeroAddress,
                nameof(BasicContractZero.ReleaseApprovedContract), new ReleaseContractInput
                {
                    ProposalId = proposalId,
                    ProposedContractInputHash = proposedContractInputHash
                });
            releaseAlreadyApprovedContractTxResult.Status.ShouldBe(TransactionResultStatus.Failed);
            releaseAlreadyApprovedContractTxResult.Error.ShouldContain("Invalid contract proposing status.");
        }

        await ApproveWithMinersAsync(Tester, ParliamentAddress, codeCheckProposalId);

        {
            var releaseNotProposedContract = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.ReleaseCodeCheckedContract),
                new ReleaseContractInput
                {
                    ProposedContractInputHash = HashHelper.ComputeFrom("Random"),
                    ProposalId = codeCheckProposalId
                });
            releaseNotProposedContract.Status.ShouldBe(TransactionResultStatus.Failed);
            releaseNotProposedContract.Error.ShouldContain("Invalid contract proposing status.");
        }

        {
            var contractCodeCheckController = await GetContractCodeCheckController(Tester, BasicContractZeroAddress);

            var deploymentProposalId = await CreateProposalAsync(Tester, ParliamentAddress,
                contractCodeCheckController.OwnerAddress,
                nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.DeploySmartContract),
                contractDeploymentInput);
            await ApproveWithMinersAsync(Tester, ParliamentAddress, deploymentProposalId);
            var releaseTx = await ReleaseProposalAsync(Tester, ParliamentAddress, deploymentProposalId);
            releaseTx.Status.ShouldBe(TransactionResultStatus.Failed);
            releaseTx.Error.ShouldContain("Invalid contract proposing status.");
        }

        {
            var incorrectContractDeploymentInput = new ContractDeploymentInput
            {
                Category = KernelConstants.CodeCoverageRunnerCategory, // test the default runner
                Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("TokenConverter")).Value)
            };

            var contractCodeCheckController = await GetContractCodeCheckController(Tester, BasicContractZeroAddress);

            var deploymentProposalId = await CreateProposalAsync(Tester, ParliamentAddress,
                contractCodeCheckController.OwnerAddress,
                nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.DeploySmartContract),
                incorrectContractDeploymentInput);
            await ApproveWithMinersAsync(Tester, ParliamentAddress, deploymentProposalId);
            var releaseTx = await ReleaseProposalAsync(Tester, ParliamentAddress, deploymentProposalId);
            releaseTx.Status.ShouldBe(TransactionResultStatus.Failed);
            releaseTx.Error.ShouldContain("Contract proposing data not found.");
        }


        {
            var releaseCodeCheckWithWrongSenderTx = await Tester.GenerateTransactionAsync(BasicContractZeroAddress,
                nameof(BasicContractZero.ReleaseCodeCheckedContract), AnotherMinerKeyPair, new ReleaseContractInput
                {
                    ProposalId = proposalId,
                    ProposedContractInputHash = proposedContractInputHash
                });
            var blockReturnSet = await Tester.MineAsync(new List<Transaction>
                { releaseCodeCheckWithWrongSenderTx });
            var noPermissionProposingTxResult =
                blockReturnSet.TransactionResultMap[releaseCodeCheckWithWrongSenderTx.GetHash()];
            noPermissionProposingTxResult.Status.ShouldBe(TransactionResultStatus.Failed);
            noPermissionProposingTxResult.Error.ShouldContain("Invalid contract proposing status.");
        }

        // release code check proposal and deployment completes
        var deploymentResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.ReleaseCodeCheckedContract),
            new ReleaseContractInput
                { ProposedContractInputHash = proposedContractInputHash, ProposalId = codeCheckProposalId });
        deploymentResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var creator = ContractDeployed.Parser.ParseFrom(deploymentResult.Logs[1].Indexed[0]).Author;
        creator.ShouldBe(BasicContractZeroAddress);
        var deployAddress = ContractDeployed.Parser.ParseFrom(deploymentResult.Logs[1].NonIndexed).Address;
        deployAddress.ShouldNotBeNull();

        var contractVersion = ContractDeployed.Parser.ParseFrom(deploymentResult.Logs[1].NonIndexed).Version;
        contractVersion.ShouldBe(1);
        var contractInfo = ContractInfo.Parser.ParseFrom(await Tester.CallContractMethodAsync(BasicContractZeroAddress,
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.GetContractInfo), deployAddress));
        contractInfo.Version.ShouldBe(1);
        contractInfo.Author.ShouldBe(BasicContractZeroAddress);

        {
            var releaseContractAlreadyFinished = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.ReleaseCodeCheckedContract),
                new ReleaseContractInput
                {
                    ProposedContractInputHash = proposedContractInputHash,
                    ProposalId = codeCheckProposalId
                });
            releaseContractAlreadyFinished.Status.ShouldBe(TransactionResultStatus.Failed);
            releaseContractAlreadyFinished.Error.ShouldContain("Invalid contract proposing status.");
        }
    }

    [Fact]
    public async Task Propose_MultiTimes()
    {
        var contractDeploymentInput = new ContractDeploymentInput
        {
            Category = KernelConstants.DefaultRunnerCategory, // test the default runner
            Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("TokenConverter")).Value)
        };

        var utcNow = TimestampHelper.GetUtcNow();
        // propose contract code
        var proposingTxResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
            nameof(BasicContractZero.ProposeNewContract), contractDeploymentInput, utcNow);
        proposingTxResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var proposalId = ProposalCreated.Parser
            .ParseFrom(proposingTxResult.Logs.First(l => l.Name.Contains(nameof(ProposalCreated))).NonIndexed)
            .ProposalId;
        proposalId.ShouldNotBeNull();
        var proposedContractInputHash = ContractProposed.Parser
            .ParseFrom(proposingTxResult.Logs.First(l => l.Name.Contains(nameof(ContractProposed))).NonIndexed)
            .ProposedContractInputHash;

        var secondProposingTxResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
            nameof(BasicContractZero.ProposeNewContract), contractDeploymentInput);
        secondProposingTxResult.Status.ShouldBe(TransactionResultStatus.Failed);

        var thirdProposingTxResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
            nameof(BasicContractZero.ProposeNewContract), contractDeploymentInput, utcNow.AddSeconds(86399));
        thirdProposingTxResult.Status.ShouldBe(TransactionResultStatus.Failed);

        var forthProposingTxResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
            nameof(BasicContractZero.ProposeNewContract), contractDeploymentInput, utcNow.AddSeconds(86400));
        forthProposingTxResult.Status.ShouldBe(TransactionResultStatus.Mined);
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
            var contractInfo = ContractInfo.Parser.ParseFrom(await Tester.CallContractMethodAsync(
                BasicContractZeroAddress,
                nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.GetContractInfo), address));
            contractInfo.Version.ShouldBe(1);
        }

        {
            // Deployment of the same contract code will fail and return null address
            var address = await DeployAsync(Tester, ParliamentAddress, contractDeploymentInput);
            address.ShouldBeNull();
        }

        {
            var newContractDeploymentInput = new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("Configuration")).Value)
            };

            var otherTester = Tester.CreateNewContractTester(AnotherUserKeyPair);
            var proposingTxResult = await otherTester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZero.ProposeNewContract), newContractDeploymentInput);
            proposingTxResult.Status.ShouldBe(TransactionResultStatus.Failed);
            proposingTxResult.Error.ShouldContain("Unauthorized to propose.");
        }
    }

    [Fact]
    public async Task UpdateSmartContract_SameCode_Test()
    {
        var contractDeploymentInput = new ContractDeploymentInput
        {
            Category = KernelConstants.DefaultRunnerCategory, // test the default runner
            Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("TokenConverter")).Value)
        };

        var newAddress = await DeployAsync(Tester, ParliamentAddress, contractDeploymentInput);
        var contractInfo = ContractInfo.Parser.ParseFrom(await Tester.CallContractMethodAsync(BasicContractZeroAddress,
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.GetContractInfo), newAddress));
        contractInfo.Version.ShouldBe(1);
        var contractUpdateInput = new ContractUpdateInput
        {
            Address = newAddress,
            Code = contractDeploymentInput.Code
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
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.ReleaseCodeCheckedContract),
            new ReleaseContractInput
                { ProposedContractInputHash = proposedContractInputHash, ProposalId = codeCheckProposalId });
        updateResult.Status.ShouldBe(TransactionResultStatus.Failed);
        updateResult.Error.ShouldContain("Code is not changed.");
    }

    [Fact]
    public async Task UpdateSmartContract_NewCodeExists_Test()
    {
        var contractDeploymentInput = new ContractDeploymentInput
        {
            Category = KernelConstants.DefaultRunnerCategory, // test the default runner
            Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("TokenConverter")).Value)
        };


        var newAddress = await DeployAsync(Tester, ParliamentAddress, contractDeploymentInput);
        var contractInfo = ContractInfo.Parser.ParseFrom(await Tester.CallContractMethodAsync(BasicContractZeroAddress,
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.GetContractInfo), newAddress));
        contractInfo.Version.ShouldBe(1);

        var contractDeploymentInput2 = new ContractDeploymentInput
        {
            Category = KernelConstants.DefaultRunnerCategory, // test the default runner
            Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("Genesis")).Value)
        };
        await DeployAsync(Tester, ParliamentAddress, contractDeploymentInput2);
        var contractUpdateInput = new ContractUpdateInput
        {
            Address = newAddress,
            Code = contractDeploymentInput2.Code
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
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.ReleaseCodeCheckedContract),
            new ReleaseContractInput
                { ProposedContractInputHash = proposedContractInputHash, ProposalId = codeCheckProposalId });
        updateResult.Status.ShouldBe(TransactionResultStatus.Failed);
        updateResult.Error.ShouldContain("Same code has been deployed before.");
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
        var contractInfo = ContractInfo.Parser.ParseFrom(await Tester.CallContractMethodAsync(BasicContractZeroAddress,
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.GetContractInfo), newAddress));
        contractInfo.Version.ShouldBe(1);
        var code = Codes.Single(kv => kv.Key.Contains("Treasury")).Value;
        var contractUpdateInput = new ContractUpdateInput
        {
            Address = newAddress,
            Code = ByteString.CopyFrom(code)
        };

        {
            var addressNotExistProposingTxResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(BasicContractZero.ProposeUpdateContract), new ContractUpdateInput
                {
                    Address = AnotherMinerAddress,
                    Code = ByteString.CopyFrom(code)
                });
            addressNotExistProposingTxResult.Status.ShouldBe(TransactionResultStatus.Failed);
            addressNotExistProposingTxResult.Error.ShouldContain("Contract not found.");
        }

        var proposingTxResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
            nameof(BasicContractZero.ProposeUpdateContract), contractUpdateInput);
        proposingTxResult.Status.ShouldBe(TransactionResultStatus.Mined);

        var secondTxProposingResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
            nameof(BasicContractZero.ProposeUpdateContract), contractUpdateInput);
        secondTxProposingResult.Status.ShouldBe(TransactionResultStatus.Failed);

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
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.ReleaseCodeCheckedContract),
            new ReleaseContractInput
                { ProposedContractInputHash = proposedContractInputHash, ProposalId = codeCheckProposalId });
        updateResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var contractAddress = CodeUpdated.Parser
            .ParseFrom(updateResult.Logs.First(l => l.Name.Contains(nameof(CodeUpdated))).Indexed[0]).Address;
        contractAddress.ShouldBe(newAddress);
        var codeHash = HashHelper.ComputeFrom(code);
        var newHash = CodeUpdated.Parser
            .ParseFrom(updateResult.Logs.First(l => l.Name.Contains(nameof(CodeUpdated))).NonIndexed).NewCodeHash;
        newHash.ShouldBe(codeHash);
        var version = CodeUpdated.Parser
            .ParseFrom(updateResult.Logs.First(l => l.Name.Contains(nameof(CodeUpdated))).NonIndexed).Version;
        version.ShouldBe(contractInfo.Version + 1);
        var updateContractInfo = ContractInfo.Parser.ParseFrom(await Tester.CallContractMethodAsync(
            BasicContractZeroAddress,
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.GetContractInfo), newAddress));
        updateContractInfo.Version.ShouldBe(contractInfo.Version + 1);

        var thirdTxProposingResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
            nameof(BasicContractZero.ProposeUpdateContract), contractUpdateInput,
            TimestampHelper.GetUtcNow().AddSeconds(86400));
        thirdTxProposingResult.Status.ShouldBe(TransactionResultStatus.Mined);
    }

    [Fact(Skip = "Skip due to need very long task delay.")]
    public async Task DeploySmartContractWithCodeCheck_Test()
    {
        var contractPatcher = GetRequiredService<IContractPatcher>();
        var contractCode =
            contractPatcher.Patch(ReadCode(Path.Combine(BaseDir, "AElf.Contracts.MultiToken.dll")), true);
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


        // Mine a block, should include approval transaction
        var block = await Tester.MineEmptyBlockAsync();
        var txs = await Tester.GetTransactionsAsync(block.TransactionIds);
        var parliamentTxs = txs.Where(tx => tx.To == ParliamentAddress).ToList();
        parliamentTxs[0].MethodName
            .ShouldBe(nameof(ParliamentContractImplContainer.ParliamentContractImplStub.ApproveMultiProposals));
    }

    [Fact(Skip = "Skip due to need task delay.")]
    public async Task UpdateSmartContractWithCodeCheck_Test()
    {
        var contractCode = ReadCode(Path.Combine(BaseDir, "AElf.Contracts.TokenConverter.dll.patched"));
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
            .ShouldBe(nameof(ParliamentContractImplContainer.ParliamentContractImplStub.ApproveMultiProposals));
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
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.ReleaseCodeCheckedContract),
            new ReleaseContractInput
                { ProposedContractInputHash = proposedContractInputHash, ProposalId = codeCheckProposalId });

        result.Status.ShouldBe(TransactionResultStatus.Mined);

        var address = CodeUpdated.Parser.ParseFrom(result.Logs[1].Indexed[0]).Address;
        address.ShouldBe(BasicContractZeroAddress);
        var codeHash = CodeUpdated.Parser.ParseFrom(result.Logs[1].NonIndexed).NewCodeHash;
        codeHash.ShouldBe(HashHelper.ComputeFrom(code));
        var contractVersion = CodeUpdated.Parser.ParseFrom(result.Logs[1].NonIndexed).Version;
        contractVersion.ShouldBe(2);
        var contractInfo = ContractInfo.Parser.ParseFrom(await Tester.CallContractMethodAsync(BasicContractZeroAddress,
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.GetContractInfo),
            BasicContractZeroAddress));
        contractInfo.Version.ShouldBe(2);
    }

    [Fact]
    public async Task ChangeContractZeroOwner_Test_Invalid_Address()
    {
        var address = Tester.GetCallOwnerAddress();
        var contractDeploymentController = await GetContractDeploymentController(Tester, BasicContractZeroAddress);
        const string proposalCreationMethodName =
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.ChangeContractDeploymentController);
        var proposalId = await CreateProposalAsync(Tester, contractDeploymentController.ContractAddress,
            contractDeploymentController.OwnerAddress, proposalCreationMethodName,
            new AuthorityInfo
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
            nameof(ParliamentContractImplContainer.ParliamentContractImplStub.CreateOrganization),
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
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.ChangeContractDeploymentController);
        var proposalId = await CreateProposalAsync(Tester, contractDeploymentController.ContractAddress,
            contractDeploymentController.OwnerAddress, proposalCreationMethodName,
            new AuthorityInfo
            {
                OwnerAddress = organizationAddress,
                ContractAddress = ParliamentAddress
            });
        await ApproveWithMinersAsync(Tester, ParliamentAddress, proposalId);
        var txResult2 = await ReleaseProposalAsync(Tester, ParliamentAddress, proposalId);
        txResult2.Status.ShouldBe(TransactionResultStatus.Mined);

        // test deployment with only one miner
        var contractDeploymentInput = new ContractDeploymentInput
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
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.ReleaseCodeCheckedContract),
            new ReleaseContractInput
                { ProposedContractInputHash = proposedContractInputHash, ProposalId = codeCheckProposalId });

        var creator = ContractDeployed.Parser
            .ParseFrom(deploymentResult.Logs.First(l => l.Name.Contains(nameof(ContractDeployed))).Indexed[0])
            .Author;

        creator.ShouldBe(BasicContractZeroAddress);

        var deployAddress = ContractDeployed.Parser
            .ParseFrom(deploymentResult.Logs.First(l => l.Name.Contains(nameof(ContractDeployed))).NonIndexed)
            .Address;

        deployAddress.ShouldNotBeNull();

        var author = Address.Parser.ParseFrom(await Tester.CallContractMethodAsync(BasicContractZeroAddress,
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.GetContractAuthor), deployAddress));

        author.ShouldBe(BasicContractZeroAddress);
    }

    [Fact]
    public async Task ChangeContractZeroOwnerByAssociation_Test()
    {
        var createOrganizationResult = await Tester.ExecuteContractWithMiningAsync(AssociationContractAddress,
            nameof(AssociationContractImplContainer.AssociationContractImplStub.CreateOrganization),
            new Association.CreateOrganizationInput
            {
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MinimalApprovalThreshold = 1,
                    MinimalVoteThreshold = 1
                },
                ProposerWhiteList = new ProposerWhiteList
                {
                    Proposers = { AnotherMinerAddress }
                },
                OrganizationMemberList = new OrganizationMemberList
                {
                    OrganizationMembers = { AnotherMinerAddress }
                }
            });

        var organizationAddress = Address.Parser.ParseFrom(createOrganizationResult.ReturnValue);

        var contractDeploymentController = await GetContractDeploymentController(Tester, BasicContractZeroAddress);
        const string proposalCreationMethodName =
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.ChangeContractDeploymentController);
        var proposalId = await CreateProposalAsync(Tester, contractDeploymentController.ContractAddress,
            contractDeploymentController.OwnerAddress, proposalCreationMethodName,
            new AuthorityInfo
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
        var contractDeploymentInput = new ContractDeploymentInput
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
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.ReleaseCodeCheckedContract),
            new ReleaseContractInput
                { ProposedContractInputHash = proposedContractInputHash, ProposalId = codeCheckProposalId });

        var creator = ContractDeployed.Parser
            .ParseFrom(deploymentResult.Logs.First(l => l.Name.Contains(nameof(ContractDeployed))).Indexed[0])
            .Author;

        creator.ShouldBe(AnotherMinerAddress);

        var deployAddress = ContractDeployed.Parser
            .ParseFrom(deploymentResult.Logs.First(l => l.Name.Contains(nameof(ContractDeployed))).NonIndexed)
            .Address;
        var author = Address.Parser.ParseFrom(await Tester.CallContractMethodAsync(BasicContractZeroAddress,
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.GetContractAuthor), deployAddress));

        author.ShouldBe(AnotherMinerAddress);
    }

    [Fact]
    public async Task DeploySmartContracts_WithoutAuth_Test()
    {
        var txResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
            nameof(ACS0Container.ACS0Stub.DeploySmartContract), new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory,
                Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("MultiToken")).Value)
            });

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
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.ReleaseCodeCheckedContract),
            new ReleaseContractInput
                { ProposedContractInputHash = proposedContractInputHash, ProposalId = codeCheckProposalId });

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
            nameof(ACS0Container.ACS0Stub.UpdateSmartContract),
            new ContractUpdateInput
            {
                Address = ParliamentAddress,
                Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("Consensus")).Value)
            });

        result.Status.ShouldBe(TransactionResultStatus.Failed);
        result.Error.Contains("Unauthorized behavior.").ShouldBeTrue();
    }

    [Fact]
    public async Task ChangeContractZeroOwner_WithoutAuth_Test()
    {
        var result = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.ChangeContractDeploymentController),
            new AuthorityInfo
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
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.ReleaseCodeCheckedContract),
            new ReleaseContractInput
                { ProposedContractInputHash = proposedContractInputHash, ProposalId = codeCheckProposalId });

        deploymentResult.Status.ShouldBe(TransactionResultStatus.Mined);
        var creator = ContractDeployed.Parser.ParseFrom(deploymentResult.Logs[1].Indexed[0]).Author;
        creator.ShouldBe(SideChainTester.GetCallOwnerAddress());
        var deployAddress = ContractDeployed.Parser.ParseFrom(deploymentResult.Logs[1].NonIndexed).Address;
        deployAddress.ShouldNotBeNull();

        var author = Address.Parser.ParseFrom(await SideChainTester.CallContractMethodAsync(
            SideBasicContractZeroAddress,
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.GetContractAuthor), deployAddress));

        author.ShouldBe(SideChainTester.GetCallOwnerAddress());

        {
            var noPermissionProposingTx = await SideChainTester.GenerateTransactionAsync(SideBasicContractZeroAddress,
                nameof(BasicContractZero.ProposeUpdateContract), AnotherMinerKeyPair, new ContractUpdateInput
                {
                    Address = deployAddress,
                    Code = ByteString.Empty
                });
            var blockReturnSet = await SideChainTester.MineAsync(new List<Transaction> { noPermissionProposingTx });
            var noPermissionProposingTxResult =
                blockReturnSet.TransactionResultMap[noPermissionProposingTx.GetHash()];
            noPermissionProposingTxResult.Status.ShouldBe(TransactionResultStatus.Failed);
            noPermissionProposingTxResult.Error.ShouldContain("No permission.");
        }
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

    [Fact]
    public async Task ChangeMethodFeeController_Test()
    {
        var createOrganizationResult = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
            nameof(ParliamentContractImplContainer.ParliamentContractImplStub.CreateOrganization),
            new CreateOrganizationInput
            {
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MinimalApprovalThreshold = 1000,
                    MinimalVoteThreshold = 1000
                }
            });

        var organizationAddress = Address.Parser.ParseFrom(createOrganizationResult.ReturnValue);

        var methodFeeController = await GetMethodFeeController(Tester, BasicContractZeroAddress);
        const string proposalCreationMethodName =
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.ChangeMethodFeeController);
        var proposalId = await CreateProposalAsync(Tester, methodFeeController.ContractAddress,
            methodFeeController.OwnerAddress, proposalCreationMethodName,
            new AuthorityInfo
            {
                OwnerAddress = organizationAddress,
                ContractAddress = ParliamentAddress
            });
        await ApproveWithMinersAsync(Tester, ParliamentAddress, proposalId);
        var txResult2 = await ReleaseProposalAsync(Tester, ParliamentAddress, proposalId);
        txResult2.Status.ShouldBe(TransactionResultStatus.Mined);

        var newMethodFeeController = await GetMethodFeeController(Tester, BasicContractZeroAddress);
        Assert.True(newMethodFeeController.OwnerAddress == organizationAddress);
    }

    [Fact]
    public async Task ChangeMethodFeeController_WithoutAuth_Test()
    {
        var result = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.ChangeMethodFeeController),
            new AuthorityInfo
            {
                OwnerAddress = Tester.GetCallOwnerAddress(),
                ContractAddress = ParliamentAddress
            });

        result.Status.ShouldBe(TransactionResultStatus.Failed);
        result.Error.Contains("Unauthorized behavior.").ShouldBeTrue();

        // Invalid organization address
        var methodFeeController = await GetMethodFeeController(Tester, BasicContractZeroAddress);
        const string proposalCreationMethodName =
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.ChangeMethodFeeController);
        var proposalId = await CreateProposalAsync(Tester, methodFeeController.ContractAddress,
            methodFeeController.OwnerAddress, proposalCreationMethodName,
            new AuthorityInfo
            {
                OwnerAddress = SampleAddress.AddressList[4],
                ContractAddress = ParliamentAddress
            });
        await ApproveWithMinersAsync(Tester, ParliamentAddress, proposalId);
        var txResult2 = await ReleaseProposalAsync(Tester, ParliamentAddress, proposalId);
        txResult2.Status.ShouldBe(TransactionResultStatus.Failed);
        txResult2.Error.Contains("Invalid authority input.").ShouldBeTrue();
    }

    [Fact]
    public async Task ChangeMethodFeeControllerByAssociation_Test()
    {
        var createOrganizationResult = await Tester.ExecuteContractWithMiningAsync(AssociationContractAddress,
            nameof(AssociationContractImplContainer.AssociationContractImplStub.CreateOrganization),
            new Association.CreateOrganizationInput
            {
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MinimalApprovalThreshold = 1,
                    MinimalVoteThreshold = 1
                },
                ProposerWhiteList = new ProposerWhiteList
                {
                    Proposers = { AnotherMinerAddress }
                },
                OrganizationMemberList = new OrganizationMemberList
                {
                    OrganizationMembers = { AnotherMinerAddress }
                }
            });

        var organizationAddress = Address.Parser.ParseFrom(createOrganizationResult.ReturnValue);

        var methodFeeController = await GetMethodFeeController(Tester, BasicContractZeroAddress);
        const string proposalCreationMethodName =
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.ChangeMethodFeeController);
        var proposalId = await CreateProposalAsync(Tester, methodFeeController.ContractAddress,
            methodFeeController.OwnerAddress, proposalCreationMethodName,
            new AuthorityInfo
            {
                OwnerAddress = organizationAddress,
                ContractAddress = AssociationContractAddress
            });
        await ApproveWithMinersAsync(Tester, ParliamentAddress, proposalId);
        var txResult2 = await ReleaseProposalAsync(Tester, ParliamentAddress, proposalId);
        txResult2.Status.ShouldBe(TransactionResultStatus.Mined);

        var methodFeeControllerAfterChange =
            await GetMethodFeeController(Tester, BasicContractZeroAddress);

        methodFeeControllerAfterChange.ContractAddress.ShouldBe(AssociationContractAddress);
        methodFeeControllerAfterChange.OwnerAddress.ShouldBe(organizationAddress);
    }

    [Fact]
    public async Task ChangeCodeCheckController_Test()
    {
        var createOrganizationResult = await Tester.ExecuteContractWithMiningAsync(ParliamentAddress,
            nameof(ParliamentContractImplContainer.ParliamentContractImplStub.CreateOrganization),
            new CreateOrganizationInput
            {
                ProposalReleaseThreshold = new ProposalReleaseThreshold
                {
                    MinimalApprovalThreshold = 1000,
                    MinimalVoteThreshold = 1000
                }
            });
        var organizationAddress = Address.Parser.ParseFrom(createOrganizationResult.ReturnValue);

        var byteResult = await Tester.CallContractMethodAsync(BasicContractZeroAddress,
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.GetCodeCheckController),
            new Empty());
        var codeCheckController = AuthorityInfo.Parser.ParseFrom(byteResult);

        const string proposalCreationMethodName =
            nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.ChangeCodeCheckController);

        {
            // organization address not exists
            var proposalId = await CreateProposalAsync(Tester, codeCheckController.ContractAddress,
                codeCheckController.OwnerAddress, proposalCreationMethodName,
                new AuthorityInfo
                {
                    OwnerAddress = TokenContractAddress,
                    ContractAddress = ParliamentAddress
                });
            await ApproveWithMinersAsync(Tester, ParliamentAddress, proposalId);
            var txResult2 = await ReleaseProposalAsync(Tester, ParliamentAddress, proposalId);
            txResult2.Status.ShouldBe(TransactionResultStatus.Failed);
            txResult2.Error.ShouldContain("Invalid authority input.");
        }

        {
            var proposalId = await CreateProposalAsync(Tester, codeCheckController.ContractAddress,
                codeCheckController.OwnerAddress, proposalCreationMethodName,
                new AuthorityInfo
                {
                    OwnerAddress = organizationAddress,
                    ContractAddress = ParliamentAddress
                });
            await ApproveWithMinersAsync(Tester, ParliamentAddress, proposalId);
            var txResult2 = await ReleaseProposalAsync(Tester, ParliamentAddress, proposalId);
            txResult2.Status.ShouldBe(TransactionResultStatus.Mined);

            byteResult = await Tester.CallContractMethodAsync(BasicContractZeroAddress,
                nameof(BasicContractZeroImplContainer.BasicContractZeroImplStub.GetCodeCheckController),
                new Empty());
            var newCodeCheckController = AuthorityInfo.Parser.ParseFrom(byteResult);
            Assert.True(newCodeCheckController.OwnerAddress == organizationAddress);
        }
    }

    #endregion
}