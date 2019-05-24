using System;
using System.Linq;
using System.Threading.Tasks;
using Acs0;
using AElf.Contracts.ParliamentAuth;
using AElf.Kernel;
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
        public async Task Initialize_AlreadyExist()
        {
            var txResult = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(ACS0Container.ACS0Stub.Initialize), (new ContractZeroInitializationInput
                {
                    ContractDeploymentAuthorityRequired = true,
                    ZeroOwnerAddressGenerationMethodName = "",
                    ZeroOwnerAddressGenerationContractHashName = Hash.FromString("")
                }));

            txResult.Status.ShouldBe(TransactionResultStatus.Failed);
            txResult.Error.Contains("Contract zero already initialized").ShouldBeTrue();
        }

        [Fact]
        public async Task DeploySmartContracts()
        {
            //Can't get the DeployContractAddress
            var contractDeploymentInput = new ContractDeploymentInput
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("MultiToken")).Value)
            };
            string methodName = "DeploySmartContract";
            //create proposal to deploy
            var proposalId = CreateProposal(methodName, contractDeploymentInput);
            //approve and release
            var txResult = await ApproveWithMiners(proposalId.Result);

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
            var proposalId = CreateProposal(methodName, contractUpdateInput);
            //approve and release
            var txResult = await ApproveWithMiners(proposalId.Result);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task ChangeContractZeroOwner()
        {
            var address = Tester.GetCallOwnerAddress();
            var methodName = "ChangeContractZeroOwner";
            //create proposal to update
            var proposalId = CreateProposal(methodName, address);
            //approve and release
            var txResult = await ApproveWithMiners(proposalId.Result);
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
            txResult.Error.Contains("Unauthorized to do this.").ShouldBeTrue();
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
            result.Error.Contains("Unauthorized to do this.").ShouldBeTrue();
        }

        [Fact]
        public async Task ChangeContractZeroOwner_WithoutAuth()
        {
            var result = await Tester.ExecuteContractWithMiningAsync(BasicContractZeroAddress,
                nameof(ACS0Container.ACS0Stub.ChangeContractZeroOwner), Tester.GetCallOwnerAddress());
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Unauthorized to do this.").ShouldBeTrue();
        }
    }
}