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
        public async Task<Address> Deploy_SmartContracts_Test()
        {
            var result = await DefaultTester.DeploySmartContract.SendAsync(new ContractDeploymentInput()
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("MultiToken")).Value)
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            result.Output.ShouldNotBeNull();
            return result.Output;
        }

        [Fact]
        public async Task Query_SmartContracts_Info_Test()
        {
            var contractAddress = await Deploy_SmartContracts_Test();

            var resultSerialNumber = await DefaultTester.CurrentContractSerialNumber.CallAsync(new Empty());
            resultSerialNumber.Value.ShouldNotBe(0UL);

            {
                var resultInfo = await DefaultTester.GetContractInfo.CallAsync(contractAddress);
                resultInfo.ShouldNotBeNull();
                resultInfo.Author.ShouldBe(DefaultSender);
            }

            {
                var resultHash = await DefaultTester.GetContractHash.CallAsync(contractAddress);
                var contractCode = Codes.Single(kv => kv.Key.Contains("MultiToken")).Value;
                var contractHash = Hash.FromRawBytes(contractCode);
                resultHash.ShouldBe(contractHash);
            }

            {
                var author = await DefaultTester.GetContractAuthor.CallAsync(contractAddress);
                author.ShouldBe(DefaultSender);
            }
        }

        [Fact]
        public async Task Query_ContractRegistrationByAddress_Test()
        {
            //not exist
            {
                var address = SampleAddress.AddressList[0];
                var registrationInfo =
                    await DefaultTester.GetSmartContractRegistrationByAddress.CallAsync(address);
                registrationInfo.ShouldBe(new SmartContractRegistration());
            }
            
            //exist contract
            {
                var registrationInfo =
                    await DefaultTester.GetSmartContractRegistrationByAddress.CallAsync(ContractZeroAddress);
            
                registrationInfo.Category.ShouldBe(KernelConstants.CodeCoverageRunnerCategory);
                registrationInfo.CodeHash.ShouldNotBeNull();
                registrationInfo.Code.Length.ShouldBeGreaterThan(0);
            }
        }

        [Fact]
        public async Task Update_SmartContract_Test()
        {
            var contractAddress = await Deploy_SmartContracts_Test();

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
        public async Task Change_GenesisOwner_Test()
        {
            //without permission
            {
                var changeResult = await DefaultTester.ChangeGenesisOwner.SendAsync(AnotherUser);
                changeResult.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
                changeResult.TransactionResult.Error.ShouldContain("Unauthorized to initialize");
            }
        }
        
        [Fact]
        public async Task Update_SmartContract_WrongAuthor_Test()
        {
            var contractAddress = await Deploy_SmartContracts_Test();

            var resultUpdate = await AnotherTester.UpdateSmartContract.SendAsync(
                new ContractUpdateInput()
                {
                    Address = contractAddress,
                    Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("Consensus")).Value),
                });
            resultUpdate.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            resultUpdate.TransactionResult.Error.Contains("Only author can propose contract update.").ShouldBeTrue();
        }

        [Fact]
        public async Task Update_SmartContract_With_Same_Code_Test()
        {
            var contractAddress = await Deploy_SmartContracts_Test();

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
        public async Task Change_Contract_Author_Test()
        {
            var contractAddress = await Deploy_SmartContracts_Test();

            var resultChange = await DefaultTester.ChangeContractAuthor.SendAsync(
                new ChangeContractAuthorInput()
                {
                    ContractAddress = contractAddress,
                    NewAuthor = AnotherUser
                });
            resultChange.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);

            var resultAuthor = await DefaultTester.GetContractAuthor.CallAsync(contractAddress);
            resultAuthor.ShouldBe(AnotherUser);
        }

        [Fact]
        public async Task Change_Contract_Author_Without_Permission_Test()
        {
            var contractAddress = await Deploy_SmartContracts_Test();
            var result = await AnotherTester.ChangeContractAuthor.SendAsync(
                new ChangeContractAuthorInput()
                {
                    ContractAddress = contractAddress,
                    NewAuthor = AnotherUser
                });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.Contains("no permission.").ShouldBeTrue();
        }
    }
}