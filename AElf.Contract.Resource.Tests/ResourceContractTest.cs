using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Genesis;
using AElf.Contracts.TestBase;
using AElf.Contracts.Token;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
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
            AsyncHelper.RunSync(() => Tester.InitialChainAsync(typeof(BasicContractZero),
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
        public async Task Initize_Resource_Contract()
        {
            //await Deploy_Contracts();

            var initResult = Tester.ExecuteContractWithMiningAsync(Tester.DeployedContractsAddresses[1], "Initialize",
                "ELF", "elf token", 1000_000UL, 2U);
            var chain = await Tester.GetChainAsync();
            var height = chain.BestChainHeight;
            initResult.Result.Status.ShouldBe(TransactionResultStatus.Mined);

            var feeAddress = Tester.GetAddress(FeeKeyPair);
            var resourceResult = Tester.ExecuteContractWithMiningAsync(Tester.DeployedContractsAddresses[2], "Initialize",
                Tester.DeployedContractsAddresses[1], feeAddress, feeAddress);
            resourceResult.Result.Status.ShouldBe(TransactionResultStatus.Mined);
        }
    }
}