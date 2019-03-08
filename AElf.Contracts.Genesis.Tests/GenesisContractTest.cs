using System.IO;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Resource;
using AElf.Contracts.TestBase;
using AElf.Contracts.Token;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.KernelAccount;
using Shouldly;
using Volo.Abp.Threading;
using Xunit;

namespace AElf.Contracts.Genesis
{
    public class BasicContractZeroTest : BasicContractZeroTestBase
    {
        private ContractTester Tester;
        private ECKeyPair otherOwnerKeyPair;

        private Address BasicZeroContractAddress;
        private Address TokenContractAddress;
        private Address _contractAddress;

        public BasicContractZeroTest()
        {
            Tester = new ContractTester();
            otherOwnerKeyPair = CryptoHelpers.GenerateKeyPair();
            AsyncHelper.RunSync(() => Tester.InitialChainAsync(Tester.GetDefaultContractTypes().ToArray()));
            BasicZeroContractAddress = Tester.GetZeroContractAddress();
            TokenContractAddress = Tester.GetContractAddress(typeof(TokenContract));
        }

        [Fact]
        public async Task Deploy_SmartContracts()
        {
            var resultDeploy = await Tester.ExecuteContractWithMiningAsync(BasicZeroContractAddress,
                nameof(ISmartContractZero.DeploySmartContract), 2,
                File.ReadAllBytes(typeof(TokenContract).Assembly.Location));
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
                    nameof(BasicContractZero.CurrentContractSerialNumber));
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
                await Tester.ExecuteContractWithMiningAsync(BasicZeroContractAddress, nameof(BasicContractZero.UpdateSmartContract),
                    _contractAddress, File.ReadAllBytes(typeof(ResourceContract).Assembly.Location));
            resultUpdate.Status.ShouldBe(TransactionResultStatus.Mined);

            var updateAddressArray = resultUpdate.ReturnValue.ToByteArray();
            var updateAddress = Address.FromBytes(updateAddressArray);
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
                await Tester.ExecuteContractWithMiningAsync(BasicZeroContractAddress, nameof(BasicContractZero.UpdateSmartContract),
                    TokenContractAddress,
                    File.ReadAllBytes(typeof(ResourceContract).Assembly.Location));
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Only owner is allowed to update code.").ShouldBeTrue();
        }

        [Fact]
        public async Task Update_SmartContract_With_Same_Code()
        {
            await Deploy_SmartContracts();

            var result =
                await Tester.ExecuteContractWithMiningAsync(BasicZeroContractAddress, nameof(BasicContractZero.UpdateSmartContract),
                    _contractAddress, File.ReadAllBytes(typeof(TokenContract).Assembly.Location));
            result.Status.ShouldBe(TransactionResultStatus.Failed);
            result.Error.Contains("Code is not changed.").ShouldBeTrue();
        }

        [Fact]
        public async Task Change_Contract_Owner()
        {
            await Deploy_SmartContracts();

            var resultChange = await Tester.ExecuteContractWithMiningAsync(BasicZeroContractAddress,
                nameof(BasicContractZero.ChangeContractOwner), _contractAddress, Tester.GetAddress(otherOwnerKeyPair));
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
            var resultChangeFailed = await Tester.ExecuteContractWithMiningAsync(BasicZeroContractAddress,
                nameof(BasicContractZero.ChangeContractOwner), TokenContractAddress, Tester.GetAddress(otherOwnerKeyPair));
            resultChangeFailed.Status.ShouldBe(TransactionResultStatus.Failed);
            resultChangeFailed.Error.Contains("no permission.").ShouldBeTrue();
        }
    }
}