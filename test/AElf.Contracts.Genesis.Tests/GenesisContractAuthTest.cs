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
                nameof(ACS0Container.ACS0Stub.ChangeGenesisOwner), SampleAddress.AddressList[0]);

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
            var proposalId = await CreateProposalAsync(methodName, contractDeploymentInput);
            //approve
            var txResult = await ApproveWithMinersAsync(proposalId);
            txResult.Status.ShouldBe(TransactionResultStatus.Mined);
            //release
            var txResult2 = await ReleaseProposalAsync(proposalId);
            txResult2.Status.ShouldBe(TransactionResultStatus.Mined);

            var creator = ContractDeployed.Parser.ParseFrom(txResult2.Logs[0].Indexed[0]).Creator;
            creator.ShouldBe(Tester.GetCallOwnerAddress());
            var deployAddress = ContractDeployed.Parser.ParseFrom(txResult2.Logs[0].NonIndexed).Address;
            deployAddress.ShouldNotBeNull();
        }

        [Fact]
        public async Task UpdateSmartContract()
        {
            var code = Codes.Single(kv => kv.Key.Contains("Consensus")).Value;
            var contractUpdateInput = new ContractUpdateInput
            {
                Address = TokenContractAddress,
                Code = ByteString.CopyFrom(code)
            };
            string methodName = "UpdateSmartContract";
            var proposalId = await CreateProposalAsync(methodName, contractUpdateInput);
            var txResult1 = await ApproveWithMinersAsync(proposalId);
            txResult1.Status.ShouldBe(TransactionResultStatus.Mined);
            var txResult2 = await ReleaseProposalAsync(proposalId);
            txResult2.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var contractAddress = CodeUpdated.Parser.ParseFrom(txResult2.Logs[0].Indexed[0]).Address;
            contractAddress.ShouldBe(TokenContractAddress);
            var codeHash = Hash.FromRawBytes(code);
            var newHash = CodeUpdated.Parser.ParseFrom(txResult2.Logs[0].NonIndexed).NewCodeHash;
            newHash.ShouldBe(codeHash);
        }

        [Fact]
        public async Task ChangeContractZeroOwner()
        {
            var address = Tester.GetCallOwnerAddress();
            var methodName = "ChangeGenesisOwner";
            var proposalId = await CreateProposalAsync(methodName, address);
            var txResult1 = await ApproveWithMinersAsync(proposalId);
            txResult1.Status.ShouldBe(TransactionResultStatus.Mined);
            var txResult2 = await ReleaseProposalAsync(proposalId);
            txResult2.Status.ShouldBe(TransactionResultStatus.Mined);
            
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