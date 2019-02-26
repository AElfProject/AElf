using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.Genesis;
using AElf.Contracts.TestBase;
using AElf.Contracts.Token;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Types.CSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Contracts.Resource.Tests
{
    public class ResourceContractTest: ResourceContractTestBase
    {
        private ContractTester Tester;
        private ECKeyPair FeeKeyPair;

        public ResourceContractTest()
        {
            Tester = new ContractTester();
            AsyncHelper.RunSync(() => Tester.InitialChainAsync(typeof(BasicContractZero), typeof(ConsensusContract),
                typeof(TokenContract), typeof(ResourceContract)));
            FeeKeyPair = CryptoHelpers.GenerateKeyPair();
        }

        [Fact]
        public async Task Deploy_Contracts()
        {
            var tokenTx = Tester.GenerateTransaction(Tester.DeployedContractsAddresses[0], "DeploySmartContract", 2,
                File.ReadAllBytes(typeof(TokenContract).Assembly.Location));
            var resourceTx = Tester.GenerateTransaction(Tester.DeployedContractsAddresses[0], "DeploySmartContract", 2,
                File.ReadAllBytes(typeof(ResourceContract).Assembly.Location));

            await Tester.MineABlockAsync(new List<Transaction> {tokenTx, resourceTx});
            var chain = await Tester.GetChainAsync();
            chain.LongestChainHeight.ShouldBeGreaterThanOrEqualTo(1UL);
        }

        [Fact]
        public async Task Initize_Resource()
        {
            var initResult = await Tester.ExecuteContractWithMiningAsync(Tester.DeployedContractsAddresses[2], "Initialize",
                "ELF", "elf token", 1000_000UL, 2U);
            initResult.Status.ShouldBe(TransactionResultStatus.Mined);
            var feeAddress = Tester.GetAddress(FeeKeyPair);
            var resourceResult = await Tester.ExecuteContractWithMiningAsync(Tester.DeployedContractsAddresses[3], "Initialize",
                Tester.DeployedContractsAddresses[2], feeAddress, feeAddress);
            resourceResult.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task Verify_Resource_AddressInfo()
        {
            await Initize_Resource();

            //verify result
            var tokenAddress = await Tester.CallContractMethodAsync(Tester.DeployedContractsAddresses[3], "GetElfTokenAddress");
            tokenAddress.DeserializeToString().ShouldBe(Tester.DeployedContractsAddresses[2].GetFormatted());

            var address = Tester.GetAddress(FeeKeyPair);
            var feeAddressString = address.GetFormatted();
            var feeAddress = await Tester.CallContractMethodAsync(Tester.DeployedContractsAddresses[3], "GetFeeAddress");
            feeAddress.DeserializeToString().ShouldBe(feeAddressString);

            var controllerAddress = await Tester.CallContractMethodAsync(Tester.DeployedContractsAddresses[3], "GetResourceControllerAddress");
            controllerAddress.DeserializeToString().ShouldBe(feeAddressString);
        }

        [Fact]
        public async Task Query_Rsource_ConverterInfo()
        {
            await Initize_Resource();

            var cpuConverter = await Tester.CallContractMethodAsync(Tester.DeployedContractsAddresses[3], "GetConverter", "Cpu");
            var cpuString = cpuConverter.DeserializeToString();
            var cpuObj = JsonConvert.DeserializeObject<JObject>(cpuString);
            cpuObj["ResBalance"].ToObject<ulong>().ShouldBe(1000_000UL);
            cpuObj["ResWeight"].ToObject<ulong>().ShouldBe(500_000UL);
            cpuObj["ResourceType"].ToObject<string>().ShouldBe("Cpu");
        }

        [Fact]
        public async Task IssueResource_With_Conntroller_Account()
        {
            await Initize_Resource();

            Tester.SetCallOwner(FeeKeyPair);
            var issueResult = await Tester.ExecuteContractWithMiningAsync(Tester.DeployedContractsAddresses[3],
                "IssueResource",
                "Cpu", 100_000UL);
            issueResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //check result
            var cpuConverter = await Tester.CallContractMethodAsync(Tester.DeployedContractsAddresses[3], "GetConverter", "Cpu");
            var cpuString = cpuConverter.DeserializeToString();
            var cpuObj = JsonConvert.DeserializeObject<JObject>(cpuString);
            cpuObj["ResBalance"].ToObject<ulong>().ShouldBe(1000_000UL + 100_000UL);
        }

        [Fact]
        public async Task IssueResource_WithNot_Conntroller_Account()
        {
            await Initize_Resource();

            var otherKeyPair = CryptoHelpers.GenerateKeyPair();
            Tester.SetCallOwner(otherKeyPair);
            var issueResult = await Tester.ExecuteContractWithMiningAsync(Tester.DeployedContractsAddresses[3],
                "IssueResource",
                "CPU", 100_000UL);
            issueResult.Status.ShouldBe(TransactionResultStatus.Failed);
        }

        [Fact]
        public async Task Buy_Resource_WithEnough_Token()
        {
            await Initize_Resource();
            var ownerAddress = Tester.GetAddress(Tester.CallOwner);
            var tokenBalance1 =
                await Tester.CallContractMethodAsync(Tester.DeployedContractsAddresses[2], "BalanceOf", ownerAddress);
            tokenBalance1.DeserializeToUInt64().ShouldBe(1000_000UL);
            var buyResult = await Tester.ExecuteContractWithMiningAsync(Tester.DeployedContractsAddresses[3],
                "BuyResource",
                "Cpu", 10_000UL);
            var returnMessage = buyResult.RetVal.ToStringUtf8();
            buyResult.Status.ShouldBe(TransactionResultStatus.Mined);

            //check result
            var tokenBalance2 =
                await Tester.CallContractMethodAsync(Tester.DeployedContractsAddresses[2], "BalanceOf", ownerAddress);
            tokenBalance2.DeserializeToUInt64().ShouldBe(1000_000UL - 10_000UL);

            var cpuBalance = await Tester.CallContractMethodAsync(Tester.DeployedContractsAddresses[3], "GetUserBalance",
                Tester.CallOwner, "Cpu");
            cpuBalance.DeserializeToUInt64().ShouldBeGreaterThan(0UL);
        }

        [Fact]
        public async Task Buy_Resource_WithoutEnough_Token()
        {
            await Initize_Resource();

            var noTokenKeyPair = CryptoHelpers.GenerateKeyPair();
            Tester.SetCallOwner(noTokenKeyPair);
            var buyResult = await Tester.ExecuteContractWithMiningAsync(Tester.DeployedContractsAddresses[3],
                "BuyResource",
                "Cpu", 10_000UL);
            buyResult.Status.ShouldBe(TransactionResultStatus.Failed);
        }
    }
}