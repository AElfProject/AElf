using System.Linq;
using System.Threading.Tasks;
using Acs0;
using AElf.Kernel;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Genesis
{
    public class BasicContractZeroTest : AuthorityNotRequiredBasicContractZeroTestBase
    {
        [Fact]
        public async Task<Address> Deploy_SmartContracts()
        {
            var result = await DefaultTester.DeploySmartContract.SendAsync(new ContractDeploymentInput()
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("MultiToken")).Value),
                Name = Hash.FromString("MultiToken")
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var address = DefaultTester.GetContractAddressByName.CallAsync(Hash.FromString("MultiToken")).Result;
            result.Output.ShouldBe(address);
            return result.Output;
        }
        
        [Fact]
        public async Task DeploySmartContracts_RepeatedName()
        {
            await Deploy_SmartContracts();
            var result = await DefaultTester.DeploySmartContract.SendAsync(new ContractDeploymentInput()
            {
                Category = KernelConstants.DefaultRunnerCategory, 
                Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("MultiToken")).Value),
                Name = Hash.FromString("MultiToken")
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.Contains("contract name already been registered").ShouldBeTrue();
        }


        [Fact]
        public async Task Query_SmartContracts_info()
        {
            var contractAddress = await Deploy_SmartContracts();

            var resultSerialNumber = await DefaultTester.CurrentContractSerialNumber.CallAsync(new Empty());
            resultSerialNumber.Value.ShouldNotBe(0UL);

            {
                var resultInfo = await DefaultTester.GetContractInfo.CallAsync(contractAddress);
                resultInfo.ShouldNotBeNull();
                resultInfo.Owner.ShouldBe(DefaultSender);
            }

            {
                var resultHash = await DefaultTester.GetContractHash.CallAsync(contractAddress);
                var contractCode = Codes.Single(kv => kv.Key.Contains("MultiToken")).Value;
                var contractHash = Hash.FromRawBytes(contractCode);
                resultHash.ShouldBe(contractHash);
            }

            {
                var resultOwner = await DefaultTester.GetContractOwner.CallAsync(contractAddress);
                resultOwner.ShouldBe(DefaultSender);
            }
        }

        [Fact]
        public async Task Update_SmartContract()
        {
            var contractAddress = await Deploy_SmartContracts();

            var resultUpdate = await DefaultTester.UpdateSmartContract.SendAsync(
                new ContractUpdateInput()
                {
                    Address = contractAddress,
                    Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("Consensus")).Value),
                });
            resultUpdate.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var updateAddress = resultUpdate.Output;
            updateAddress.ShouldBe(contractAddress);

            var resultHash = await DefaultTester.GetContractHash.CallAsync(updateAddress);
            var contractCode = Codes.Single(kv => kv.Key.Contains("Consensus")).Value;
            var contractHash = Hash.FromRawBytes(contractCode);
            resultHash.ShouldBe(contractHash);
        }

        [Fact]
        public async Task Update_SmartContract_With_Same_Code()
        {
            var contractAddress = await Deploy_SmartContracts();

            var result = await DefaultTester.UpdateSmartContract.SendAsync(
                new ContractUpdateInput()
                {
                    Address = contractAddress,
                    Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("MultiToken")).Value)
                });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.Contains("Code is not changed.").ShouldBeTrue();
        }

        [Fact]
        public async Task Change_Contract_Owner()
        {
            var contractAddress = await Deploy_SmartContracts();

            var resultChange = await DefaultTester.ChangeContractOwner.SendAsync(
                new ChangeContractOwnerInput()
                {
                    ContractAddress = contractAddress,
                    NewOwner = AnotherUser
                });
            resultChange.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var resultOwner = await DefaultTester.GetContractOwner.CallAsync(contractAddress);
            resultOwner.ShouldBe(AnotherUser);
        }

        [Fact]
        public async Task Change_Contract_Owner_Without_Permission()
        {
            var contractAddress = await Deploy_SmartContracts();
            var result = await AnotherTester.ChangeContractOwner.SendAsync(
                new ChangeContractOwnerInput()
                {
                    ContractAddress = contractAddress,
                    NewOwner = AnotherUser
                });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.Contains("no permission.").ShouldBeTrue();
        }
    }
}