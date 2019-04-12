using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Resource;
using AElf.Contracts.TestKit;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Application;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace AElf.Contracts.Genesis
{
    public class BasicContractZeroTest : ContractTestBase<BasicContractZeroTestAElfModule>
    {
        private ISmartContractAddressService ContractAddressService =>
            Application.ServiceProvider.GetRequiredService<ISmartContractAddressService>();
        private Address ContractZeroAddress => ContractAddressService.GetZeroSmartContractAddress();
        internal BasicContractZeroContainer.BasicContractZeroStub DefaultTester =>
            GetTester<BasicContractZeroContainer.BasicContractZeroStub>(ContractZeroAddress, DefaultSenderKeyPair);
        private ECKeyPair DefaultSenderKeyPair => SampleECKeyPairs.KeyPairs.First();
        private Address DefaultSender => Address.FromPublicKey(DefaultSenderKeyPair.PublicKey);
        private ECKeyPair AnotherUserKeyPair => SampleECKeyPairs.KeyPairs.Last();
        private Address AnotherUser => Address.FromPublicKey(AnotherUserKeyPair.PublicKey);
        internal BasicContractZeroContainer.BasicContractZeroStub AnotherTester =>
            GetTester<BasicContractZeroContainer.BasicContractZeroStub>(ContractZeroAddress, AnotherUserKeyPair);

        [Fact]
        public async Task<Address> Deploy_SmartContracts()
        {
            var result = await DefaultTester.DeploySmartContract.SendAsync(new ContractDeploymentInput()
            {
                Category = KernelConstants.DefaultRunnerCategory, // test the default runner
                Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(TokenContract).Assembly.Location))
            });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            result.Output.ShouldNotBeNull();
            return result.Output;
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
                var contractCode = File.ReadAllBytes(typeof(TokenContract).Assembly.Location);
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
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(ResourceContract).Assembly.Location))
                });
            resultUpdate.TransactionResult.Status.ShouldBe(TransactionResultStatus.Mined);
            
            var updateAddress = resultUpdate.Output;
            updateAddress.ShouldBe(contractAddress);

            var resultHash = await DefaultTester.GetContractHash.CallAsync(updateAddress);
            var contractCode = File.ReadAllBytes(typeof(ResourceContract).Assembly.Location);
            var contractHash = Hash.FromRawBytes(contractCode);
            resultHash.ShouldBe(contractHash);
        }

        [Fact]
        public async Task Update_SmartContract_Without_Owner()
        {
            var contractAddress = await Deploy_SmartContracts();
            var result = await AnotherTester.UpdateSmartContract.SendAsync(
                new ContractUpdateInput()
                {
                    Address = contractAddress,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(ResourceContract).Assembly.Location))
                });
            result.TransactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
            result.TransactionResult.Error.Contains("Only owner is allowed to update code.").ShouldBeTrue();
        }

        [Fact]
        public async Task Update_SmartContract_With_Same_Code()
        {
            var contractAddress = await Deploy_SmartContracts();

            var result = await DefaultTester.UpdateSmartContract.SendAsync(
                new ContractUpdateInput()
                {
                    Address = contractAddress,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(TokenContract).Assembly.Location))    
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