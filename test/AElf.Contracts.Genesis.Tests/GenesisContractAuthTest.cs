using System;
using System.Linq;
using System.Threading.Tasks;
using Acs0;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Genesis
{
    public class GenesisContractAuthTest : BasicContractZeroTestBase
    {
        [Fact]
        public async Task Initialize_AlreadyExist()
        {
            var txResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(ACS0Container.ACS0Stub.ChangeGenesisOwner), Address.FromString("Genesis"));

            txResult.Status.ShouldBe(TransactionResultStatus.Failed);
            txResult.Error.Contains("Unauthorized behavior.").ShouldBeTrue();
        }

        [Fact]
        public async Task DeploySmartContracts()
        {
            var contractDeploymentInput = new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("MultiToken")).Value)
            };
            string methodName = "DeploySmartContract";
            //create proposal to deploy
            var proposalId = CreateProposalAsync(methodName, contractDeploymentInput);
            //approve and release
            var txResult = await ApproveWithMinersAsync(proposalId.Result);

            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task UpdateSmartContract()
        {
            var contractUpdateInput = new ContractUpdateInput
            {
                Address = TokenContractAddress,
                Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("Consensus")).Value)
            };
            string methodName = "UpdateSmartContract";
            //create proposal to update
            var proposalId = CreateProposalAsync(methodName, contractUpdateInput);
            //approve and release
            var txResult = await ApproveWithMinersAsync(proposalId.Result);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task ChangeContractZeroOwner()
        {
            var address = Tester.GetCallOwnerAddress();
            var methodName = "ChangeGenesisOwner";
            //create proposal to update
            var proposalId = CreateProposalAsync(methodName, address);
            //approve and release
            var txResult = await ApproveWithMinersAsync(proposalId.Result);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            //check the address
            var result = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(ACS0Container.ACS0Stub.DeploySmartContract), (new ContractDeploymentInput()
                {
                    Category = KernelConstants.DefaultRunnerCategory, 
                    Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("MultiToken")).Value)
                }));
            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }


        [Fact]
        public async Task DeploySmartContracts_WithoutAuth()
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
        public async Task UpdateSmartContract_WithoutAuth()
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
        public async Task ChangeContractZeroOwner_WithoutAuth()
        {
            var result = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(ACS0Container.ACS0Stub.ChangeGenesisOwner), Tester.GetCallOwnerAddress());
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Unauthorized behavior.").ShouldBeTrue();
        }
    }
}