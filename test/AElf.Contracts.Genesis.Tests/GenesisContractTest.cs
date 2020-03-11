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
        private async Task<Address> Deploy_SmartContracts_Test()
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
        public async Task Query_ContractRegistration_Test()
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
                //query by address
                var registrationInfo =
                    await DefaultTester.GetSmartContractRegistrationByAddress.CallAsync(ContractZeroAddress);
                registrationInfo.Category.ShouldBe(KernelConstants.CodeCoverageRunnerCategory);
                registrationInfo.CodeHash.ShouldNotBeNull();
                registrationInfo.Code.Length.ShouldBeGreaterThan(0);
                
                //query by hash
                var registrationInfo1 =
                    await DefaultTester.GetSmartContractRegistration.CallAsync(registrationInfo.CodeHash);
                registrationInfo1.ShouldBe(registrationInfo);
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
        public async Task Update_SmartContract_WrongAuthor_Test()
        {
            var contractAddress = await Deploy_SmartContracts_Test();

            var resultUpdate = await AnotherTester.UpdateSmartContract.SendWithExceptionAsync(
                new ContractUpdateInput()
                {
                    Address = contractAddress,
                    Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("Consensus")).Value),
                });
            resultUpdate.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            resultUpdate.TransactionResult.Error.Contains("No permission.").ShouldBeTrue();
        }

        [Fact]
        public async Task Update_SmartContract_With_Same_Code_Test()
        {
            var contractAddress = await Deploy_SmartContracts_Test();

            var result = await DefaultTester.UpdateSmartContract.SendWithExceptionAsync(
                new ContractUpdateInput
                {
                    Address = contractAddress,
                    Code = ByteString.CopyFrom(Codes.Single(kv => kv.Key.Contains("MultiToken")).Value)
                });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.Contains("Code is not changed.").ShouldBeTrue();
        }
    }
}