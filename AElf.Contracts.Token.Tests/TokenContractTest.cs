using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Contracts.Genesis;
using AElf.Contracts.TestBase;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Types.CSharp;
using Xunit;
using Shouldly;
using Volo.Abp.Threading;

namespace AElf.Contracts.Token
{
public sealed class TokenContractTest : TokenContractTestBase
    {
        private ContractTester Tester { get; set; }

        public TokenContractTest()
        {
            Tester = new ContractTester();
            AsyncHelper.RunSync(() => Tester.InitialChainAsync(typeof(BasicContractZero), typeof(TokenContract)));
        }

        [Fact]
        public async Task Deploy_TokenContract()
        {
            var tx = Tester.GenerateTransaction(Tester.DeployedContractsAddresses[0], "DeploySmartContract", 2,
                File.ReadAllBytes(typeof(TokenContract).Assembly.Location));

            await Tester.MineABlockAsync(new List<Transaction> {tx});
            var chain = await Tester.GetChainAsync();
            chain.LongestChainHeight.ShouldBeGreaterThanOrEqualTo(1UL);
        }

        [Fact]
        public async Task Deploy_TokenContract_Twice()
        {
            var bytes1 = await Tester.CallContractMethodAsync(Tester.DeployedContractsAddresses[0], "DeploySmartContract", 2,
                File.ReadAllBytes(typeof(TokenContract).Assembly.Location));

            var otherKeyPair = CryptoHelpers.GenerateKeyPair();
            Tester.SetCallOwner(otherKeyPair);
            var bytes2 = await Tester.CallContractMethodAsync(Tester.DeployedContractsAddresses[0], "DeploySmartContract", 2,
                File.ReadAllBytes(typeof(TokenContract).Assembly.Location));

            bytes1.ShouldNotBeSameAs(bytes2);
        }

        [Fact]
        public async Task Initialize_TokenContract()
        {
            await Deploy_TokenContract();

            var tx = Tester.GenerateTransaction(Tester.DeployedContractsAddresses[1], "Initialize",
                "ELF", "elf token", 1000_000UL, 2U);
            await Tester.MineABlockAsync(new List<Transaction> {tx});
            var bytes = await Tester.CallContractMethodAsync(Tester.DeployedContractsAddresses[1], "BalanceOf", Tester.GetCallOwnerAddress());
            var result = bytes.DeserializeToUInt64();
            result.ShouldBe(1000_000UL);
        }

        [Fact]
        public async Task Initialize_View_TokenContract()
        {
            await Initialize_TokenContract();

            var bytes1 = await Tester.CallContractMethodAsync(Tester.DeployedContractsAddresses[1], "TotalSupply");
            bytes1.DeserializeToUInt64().ShouldBe(1000_000UL);
            var bytes2 = await Tester.CallContractMethodAsync(Tester.DeployedContractsAddresses[1], "Decimals");
            bytes2.DeserializeToUInt64().ShouldBe(2U);
            var byte3 = await Tester.CallContractMethodAsync(Tester.DeployedContractsAddresses[1], "TokenName");
            byte3.DeserializeToString().ShouldBe("elf token");
            var byte4 = await Tester.CallContractMethodAsync(Tester.DeployedContractsAddresses[1], "Symbol");
            byte4.DeserializeToString().ShouldBe("ELF");
        }

        [Fact]
        public async Task Initialize_TokenContract_Failed()
        {
            await Initialize_TokenContract();

            var otherKeyPair = CryptoHelpers.GenerateKeyPair();
            Tester.SetCallOwner(otherKeyPair);
            var result = await Tester.ExecuteContractWithMiningAsync(Tester.DeployedContractsAddresses[1], "Initialize",
                "ELF", "elf token", 1000_000UL, 2U);

            result.Status.ShouldBe(TransactionResultStatus.Failed);
        }

        [Fact]
        public async Task Transfer_TokenContract()
        {
            await Initialize_TokenContract();

            var toAddress = CryptoHelpers.GenerateKeyPair();
            await Tester.ExecuteContractWithMiningAsync(Tester.DeployedContractsAddresses[1], "Transfer",
                Tester.GetAddress(toAddress), 1000UL);

            var bytes1 = await Tester.CallContractMethodAsync(Tester.DeployedContractsAddresses[1], "BalanceOf", Tester.GetCallOwnerAddress());
            var bytes2 = await Tester.CallContractMethodAsync(Tester.DeployedContractsAddresses[1], "BalanceOf", Tester.GetAddress(toAddress));
            bytes1.DeserializeToUInt64().ShouldBe(1000_000UL - 1000UL);
            bytes2.DeserializeToUInt64().ShouldBe(1000UL);
        }

        [Fact]
        public async Task Transfer_Without_Enough_Token()
        {
            await Initialize_TokenContract();

            var toAddress = CryptoHelpers.GenerateKeyPair();
            var result = Tester.ExecuteContractWithMiningAsync(Tester.DeployedContractsAddresses[1], "Transfer",
                Tester.GetAddress(toAddress), 1000_000_00UL);
            result.Result.Status.ShouldBe(TransactionResultStatus.Failed);
        }

        [Fact]
        public async Task Approve_UnApprove_TokenContract()
        {
            await Initialize_TokenContract();

            var spenderKeyPair = CryptoHelpers.GenerateKeyPair();
            var owner = Tester.GetCallOwnerAddress();
            var spender = Tester.GetAddress(spenderKeyPair);

            var result1 = await Tester.ExecuteContractWithMiningAsync(Tester.DeployedContractsAddresses[1], "Approve", spender, 2000UL);
            result1.Status.ShouldBe(TransactionResultStatus.Mined);
            var bytes1 = await Tester.CallContractMethodAsync(Tester.DeployedContractsAddresses[1], "Allowance", owner, spender);
            bytes1.DeserializeToUInt64().ShouldBe(2000UL);

            var result2 =
                await Tester.ExecuteContractWithMiningAsync(Tester.DeployedContractsAddresses[1], "UnApprove", spender, 1000UL);
            result2.Status.ShouldBe(TransactionResultStatus.Mined);
            var bytes2 = await Tester.CallContractMethodAsync(Tester.DeployedContractsAddresses[1], "Allowance", owner, spender);
            bytes2.DeserializeToUInt64().ShouldBe(2000UL - 1000UL);
        }

        [Fact]
        public async Task UnApprove_Without_Enough_Allowance()
        {
            await Initialize_TokenContract();

            var spenderKeyPair = CryptoHelpers.GenerateKeyPair();
            var owner = Tester.GetCallOwnerAddress();
            var spender = Tester.GetAddress(spenderKeyPair);

            var bytes = await Tester.CallContractMethodAsync(Tester.DeployedContractsAddresses[1], "Allowance", owner, spender);
            bytes.DeserializeToUInt64().ShouldBe(0UL);
            var result = await Tester.ExecuteContractWithMiningAsync(Tester.DeployedContractsAddresses[1], "UnApprove",
                spender, 1000UL);
            result.Status.ShouldBe(TransactionResultStatus.Mined);
        }

        [Fact]
        public async Task TransferFrom_TokenContract()
        {
            await Initialize_TokenContract();

            var spenderKeyPair = CryptoHelpers.GenerateKeyPair();
            var owner = Tester.GetCallOwnerAddress();
            var spender = Tester.GetAddress(spenderKeyPair);

            var result1 = await Tester.ExecuteContractWithMiningAsync(Tester.DeployedContractsAddresses[1], "Approve", spender, 2000UL);
            result1.Status.ShouldBe(TransactionResultStatus.Mined);
            var bytes1 = await Tester.CallContractMethodAsync(Tester.DeployedContractsAddresses[1], "Allowance", owner, spender);
            bytes1.DeserializeToUInt64().ShouldBe(2000UL);

            Tester.SetCallOwner(spenderKeyPair);
            var result2 =
                await Tester.ExecuteContractWithMiningAsync(Tester.DeployedContractsAddresses[1], "TransferFrom", owner, spender,
                    1000UL);
            result2.Status.ShouldBe(TransactionResultStatus.Mined);
            var bytes2 = await Tester.CallContractMethodAsync(Tester.DeployedContractsAddresses[1], "Allowance", owner, spender);
            bytes2.DeserializeToUInt64().ShouldBe(2000UL - 1000UL);

            var bytes3 = await Tester.CallContractMethodAsync(Tester.DeployedContractsAddresses[1], "BalanceOf", spender);
            bytes3.DeserializeToUInt64().ShouldBe(1000UL);
        }

        [Fact]
        public async Task TransferFrom_With_ErrorAccount()
        {
            await Initialize_TokenContract();

            var spenderKeyPair = CryptoHelpers.GenerateKeyPair();
            var owner = Tester.GetCallOwnerAddress();
            var spender = Tester.GetAddress(spenderKeyPair);

            var result1 = await Tester.ExecuteContractWithMiningAsync(Tester.DeployedContractsAddresses[1], "Approve", spender, 2000UL);
            result1.Status.ShouldBe(TransactionResultStatus.Mined);
            var bytes1 = await Tester.CallContractMethodAsync(Tester.DeployedContractsAddresses[1], "Allowance", owner, spender);
            bytes1.DeserializeToUInt64().ShouldBe(2000UL);

            var result2 =
                await Tester.ExecuteContractWithMiningAsync(Tester.DeployedContractsAddresses[1], "TransferFrom", owner, spender,
                    1000UL);
            result2.Status.ShouldBe(TransactionResultStatus.Failed);
            var bytes2 = await Tester.CallContractMethodAsync(Tester.DeployedContractsAddresses[1], "Allowance", owner, spender);
            bytes2.DeserializeToUInt64().ShouldBe(2000UL);

            var bytes3 = await Tester.CallContractMethodAsync(Tester.DeployedContractsAddresses[1], "BalanceOf", spender);
            bytes3.DeserializeToUInt64().ShouldBe(0UL);
        }

        [Fact]
        public async Task TransferFrom_Without_Enough_Allowance()
        {
            var spenderKeyPair = CryptoHelpers.GenerateKeyPair();
            var owner = Tester.GetCallOwnerAddress();
            var spender = Tester.GetAddress(spenderKeyPair);

            var bytes = await Tester.CallContractMethodAsync(Tester.DeployedContractsAddresses[1], "Allowance", owner, spender);
            bytes.DeserializeToUInt64().ShouldBe(0UL);

            Tester.SetCallOwner(spenderKeyPair);
            var result =
                await Tester.ExecuteContractWithMiningAsync(Tester.DeployedContractsAddresses[1], "TransferFrom", owner, spender,
                    1000UL);
            result.Status.ShouldBe(TransactionResultStatus.Failed);
        }

        [Fact]
        public async Task Burn_TokenContract()
        {
            await Initialize_TokenContract();
            var toAddress = CryptoHelpers.GenerateKeyPair();
            await Tester.ExecuteContractWithMiningAsync(Tester.DeployedContractsAddresses[1], "Burn",
                3000UL);
            var bytes = await Tester.CallContractMethodAsync(Tester.DeployedContractsAddresses[1], "BalanceOf",
                Tester.GetCallOwnerAddress());
            bytes.DeserializeToUInt64().ShouldBe(1000_000UL - 3000UL);
        }
    }
}