using System.IO;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.MultiToken;
using AElf.Contracts.Resource;
using AElf.Contracts.TestBase;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.KernelAccount;
using AElf.Kernel.Token;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.Genesis
{
    public class BasicContractZeroTest : ContractTestBase<ContractTestAElfModule>
    {
        private ECKeyPair otherOwnerKeyPair;

        private Address BasicZeroContractAddress;
        private Address TokenContractAddress;
        private Address _contractAddress;

        public BasicContractZeroTest()
        {
            otherOwnerKeyPair = CryptoHelpers.GenerateKeyPair();
            AsyncHelper.RunSync(() => Tester.InitialChainAsync(Tester.GetDefaultContractTypes(Tester.GetCallOwnerAddress())));
            BasicZeroContractAddress = Tester.GetZeroContractAddress();
            TokenContractAddress = Tester.GetContractAddress(TokenSmartContractAddressNameProvider.Name);
        }

        [Fact]
        public async Task Deploy_SmartContracts()
        {
            var resultDeploy = await Tester.ExecuteContractWithMiningAsync(BasicZeroContractAddress,
                nameof(ISmartContractZero.DeploySmartContract),
                new ContractDeploymentInput()
                {
                    Category = 3,
                    Code =ByteString.CopyFrom(File.ReadAllBytes(typeof(TokenContract).Assembly.Location)) 
                });
            var contractAddressArray = resultDeploy.ReturnValue.ToByteArray();
            _contractAddress = Address.Parser.ParseFrom(contractAddressArray);
            _contractAddress.ShouldNotBeNull();
        }

        [Fact]
        public async Task Query_SmartContracts_info()
        {
            await Deploy_SmartContracts();

            var resultSerialNumber =
                await Tester.CallContractMethodAsync(BasicZeroContractAddress,
                    nameof(BasicContractZero.CurrentContractSerialNumber),
                    new Empty());
            resultSerialNumber.ShouldNotBeNull();

            var resultInfo = await Tester.CallContractMethodAsync(BasicZeroContractAddress,
                nameof(BasicContractZero.GetContractInfo), _contractAddress);
            resultInfo.ShouldNotBeNull();

            var resultHashByteString = await Tester.CallContractMethodAsync(BasicZeroContractAddress,
                nameof(BasicContractZero.GetContractHash), _contractAddress);
            var resultHash = Hash.Parser.ParseFrom(resultHashByteString);
            var contractCode = File.ReadAllBytes(typeof(TokenContract).Assembly.Location);
            var contractHash = Hash.FromRawBytes(contractCode);
            resultHash.ShouldBe(contractHash);

            var resultOwner = await Tester.CallContractMethodAsync(BasicZeroContractAddress,
                nameof(BasicContractZero.GetContractOwner), _contractAddress);
            var ownerAddressArray = resultOwner.ToByteArray();
            var ownerAddress = Address.Parser.ParseFrom(ownerAddressArray);
            ownerAddress.ShouldBe(Tester.GetCallOwnerAddress());
        }

        [Fact]
        public async Task Update_SmartContract()
        {
            await Deploy_SmartContracts();

            var resultUpdate =
                await Tester.ExecuteContractWithMiningAsync(BasicZeroContractAddress,
                    nameof(BasicContractZero.UpdateSmartContract),
                    new ContractUpdateInput()
                    {
                    Address = _contractAddress,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(ResourceContract).Assembly.Location))
                    });
            resultUpdate.Status.ShouldBe(TransactionResultStatus.Mined);

            var updateAddress = Address.Parser.ParseFrom(resultUpdate.ReturnValue);
            updateAddress.ShouldBe(_contractAddress);

            var resultHashByteString = await Tester.CallContractMethodAsync(BasicZeroContractAddress,
                nameof(BasicContractZero.GetContractHash), updateAddress);
            var resultHash = Hash.Parser.ParseFrom(resultHashByteString);
            var contractCode = File.ReadAllBytes(typeof(ResourceContract).Assembly.Location);
            var contractHash = Hash.FromRawBytes(contractCode);
            resultHash.ShouldBe(contractHash);
        }

        [Fact]
        public async Task Update_SmartContract_Without_Owner()
        {
            var result =
                await Tester.ExecuteContractWithMiningAsync(BasicZeroContractAddress,
                    nameof(BasicContractZero.UpdateSmartContract),
                    new ContractUpdateInput()
                    {
                        Address = TokenContractAddress,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(ResourceContract).Assembly.Location))
                    });
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Only owner is allowed to update code.").ShouldBeTrue();
        }

        [Fact]
        public async Task Update_SmartContract_With_Same_Code()
        {
            await Deploy_SmartContracts();

            var result =
                await Tester.ExecuteContractWithMiningAsync(
                    BasicZeroContractAddress,
                    nameof(BasicContractZero.UpdateSmartContract),
                    new ContractUpdateInput()
                    {
                        Address = _contractAddress,
                        Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(TokenContract).Assembly.Location))    
                    });
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Code is not changed.").ShouldBeTrue();
        }

        [Fact]
        public async Task Change_Contract_Owner()
        {
            await Deploy_SmartContracts();

            var resultChange = await Tester.ExecuteContractWithMiningAsync(
                BasicZeroContractAddress,
                nameof(BasicContractZero.ChangeContractOwner),
                new ChangeContractOwnerInput()
                {
                   ContractAddress = _contractAddress,
                   NewOwner = Tester.GetAddress(otherOwnerKeyPair)    
                });
            resultChange.Status.ShouldBe(TransactionResultStatus.Mined);

            var resultOwner = await Tester.CallContractMethodAsync(BasicZeroContractAddress,
                nameof(BasicContractZero.GetContractOwner), _contractAddress);
            var ownerAddressArray = resultOwner.ToByteArray();
            var ownerAddress = Address.Parser.ParseFrom(ownerAddressArray);
            ownerAddress.ShouldBe(Tester.GetAddress(otherOwnerKeyPair));
        }

        [Fact]
        public async Task Change_Contract_Owner_Without_Permission()
        {
            var resultChangeFailed = await Tester.ExecuteContractWithMiningAsync(
                BasicZeroContractAddress,
                nameof(BasicContractZero.ChangeContractOwner),
                new ChangeContractOwnerInput()
                {
                    ContractAddress = TokenContractAddress,
                    NewOwner = Tester.GetAddress(otherOwnerKeyPair)
                });
            resultChangeFailed.Status.ShouldBe(TransactionResultStatus.Failed);
            resultChangeFailed.Error.Contains("no permission.").ShouldBeTrue();
        }
    }
}