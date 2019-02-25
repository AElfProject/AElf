using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Genesis;
using AElf.Contracts.TestBase;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Types.CSharp;
using Xunit;
using Shouldly;

namespace AElf.Contracts.Token.Tests
{
public sealed class TokenContractTest : TokenContractTestBase
    {
        private int ChainId { get; } = ChainHelpers.ConvertBase58ToChainId("AELF");
        private List<Address> ContractAddresses { get; set; }
        private ContractTester Tester { get; set; }

        public TokenContractTest()
        {
            Tester = new ContractTester(ChainId);
            ContractAddresses = Tester.InitialChainAsync(typeof(BasicContractZero), typeof(TokenContract)).Result;
        }

        [Fact]
        public async Task Deploy_TokenContract()
        {
            var tx = Tester.GenerateTransaction(ContractAddresses[0], "DeploySmartContract", 2,
                File.ReadAllBytes(typeof(TokenContract).Assembly.Location));

            await Tester.MineABlockAsync(new List<Transaction> {tx});
            var chain = await Tester.GetChainAsync();
            chain.LongestChainHeight.ShouldBeGreaterThanOrEqualTo(1UL);
        }

        [Fact]
        public async Task Initialize_TokenContract()
        {
            Deploy_TokenContract();

            var tx = Tester.GenerateTransaction(ContractAddresses[1], "Initialize",
                "ELF", "elf token", 1000_000UL, 2U);
            await Tester.MineABlockAsync(new List<Transaction> {tx});

            var bytes = await Tester.ExecuteContractAsync(ContractAddresses[1], "BalanceOf", Tester.GetCallOwnerAddress());
            var result = bytes.DeserializeToUInt64();
            result.ShouldBe(1000_000UL);
        }

        [Fact]
        public async Task Transfer_TokenContract()
        {
            Initialize_TokenContract();
            var toAddress = CryptoHelpers.GenerateKeyPair();
            var tx = Tester.GenerateTransaction(ContractAddresses[1], "Transfer",
                Tester.GetAddress(toAddress), 1000UL);
            await Tester.MineABlockAsync(new List<Transaction> {tx});

            var bytes1 = await Tester.ExecuteContractAsync(ContractAddresses[1], "BalanceOf", Tester.GetCallOwnerAddress());
            var bytes2 = await Tester.ExecuteContractAsync(ContractAddresses[1], "BalanceOf", Tester.GetAddress(toAddress));
            bytes1.DeserializeToUInt64().ShouldBe(1000_000UL - 1000UL);
            bytes2.DeserializeToUInt64().ShouldBe(1000UL);
        }

        [Fact]
        public async Task Burn_TokenContract()
        {
            Initialize_TokenContract();
            var toAddress = CryptoHelpers.GenerateKeyPair();
            var tx = Tester.GenerateTransaction(ContractAddresses[1], "Burn",
                3000UL);
            await Tester.MineABlockAsync(new List<Transaction> {tx});
            var bytes1 = await Tester.ExecuteContractAsync(ContractAddresses[1], "BalanceOf", Tester.GetCallOwnerAddress());
            bytes1.DeserializeToUInt64().ShouldBe(1000_000UL - 3000UL);
        }

    }
}