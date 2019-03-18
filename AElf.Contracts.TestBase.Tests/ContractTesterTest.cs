using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.Genesis;
using AElf.Contracts.Token;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using AElf.Types.CSharp;
using Xunit;

namespace AElf.Contracts.TestBase.Tests
{
    public class ContractTesterTest
    {
        private int ChainId { get; } = ChainHelpers.ConvertBase58ToChainId("AELF");

        [Fact]
        public async Task InitialChainTest()
        {
            var tester = new ContractTester<ContractTestAElfModule>();
            await tester.InitialChainAsync();

            var chain = await tester.GetChainAsync();
            var longestChainHeight = chain.LongestChainHeight;

            Assert.Equal(1, longestChainHeight);
        }

        [Fact]
        public async Task MineTest()
        {
            var tester = new ContractTester<ContractTestAElfModule>();
            await tester.InitialChainAsync();

            var zeroContractAddress = tester.GetContractAddress(Hash.Empty);
            // Create a transaction to deploy token contract.
            var tx = await tester.GenerateTransactionAsync(zeroContractAddress, "DeploySmartContract", 2,
                File.ReadAllBytes(typeof(TokenContract).Assembly.Location));

            var chain = await tester.GetChainAsync();
            var longestChainHeight = chain.LongestChainHeight;

            Assert.Equal(1, longestChainHeight);

            await tester.MineAsync(new List<Transaction> {tx});

            chain = await tester.GetChainAsync();
            longestChainHeight = chain.LongestChainHeight;

            // The longest chain height increased from 1 to 2.
            Assert.Equal(2, longestChainHeight);
        }

        [Fact]
        public async Task MultipleNodesTest()
        {
            var tester1 = new ContractTester<ContractTestAElfModule>();
            await tester1.InitialChainAsync();

            var tester2 = new ContractTester<ContractTestAElfModule>();
            await tester2.InitialChainAsync();

            var zeroContractAddress = tester1.GetContractAddress(Hash.Empty);

            var tx = await tester1.GenerateTransactionAsync(zeroContractAddress, "DeploySmartContract", 2,
                File.ReadAllBytes(typeof(TokenContract).Assembly.Location));

            await tester1.MineAsync(new List<Transaction> {tx});

            var chain1 = await tester1.GetChainAsync();
            var chain2 = await tester2.GetChainAsync();

            // For different nodes, the chain information are different.
            Assert.Equal(2, chain1.BestChainHeight);
            Assert.Equal(1, chain2.BestChainHeight);
        }

        [Fact]
        public async Task AddABlockTest()
        {
            var tester1 = new ContractTester<ContractTestAElfModule>();
            await tester1.InitialChainAsync();

            var tester2 = new ContractTester<ContractTestAElfModule>();
            await tester2.InitialChainAsync();

            var zeroContractAddress = tester1.GetZeroContractAddress();
            var tx = await tester1.GenerateTransactionAsync(zeroContractAddress, "DeploySmartContract", 2,
                File.ReadAllBytes(typeof(TokenContract).Assembly.Location));

            var block = await tester1.MineAsync(new List<Transaction> {tx});

            await tester2.ExecuteBlock(block, new List<Transaction> {tx});

            var chain2 = await tester2.GetChainAsync();

            Assert.Equal(2, chain2.BestChainHeight);

            // Check the executing result of txs in new block.

            var txResult = await tester2.GetTransactionResultAsync(tx.GetHash());

            Assert.Equal(TransactionResultStatus.Mined, txResult.Status);
        }

        [Fact]
        public async Task CallContractTest()
        {
            // Choose token contract to do this test.
            const ulong totalSupply = 10_0000_0000;

            var callerKeyPair = CryptoHelpers.GenerateKeyPair();
            var tester = new ContractTester<ContractTestAElfModule>(ChainId, callerKeyPair);
            await tester.InitialChainAsync(list =>
            {
                list.AddGenesisSmartContract<ConsensusContract>(
                    ConsensusSmartContractAddressNameProvider.Name);
                list.AddGenesisSmartContract<TokenContract>(
                    TokenSmartContractAddressNameProvider.Name);
            }); //, typeof(ConsensusContract), typeof(TokenContract));

            var tokenContractAddress = tester.GetContractAddress(TokenSmartContractAddressNameProvider.Name);
            var txToInitialize =
                await tester.GenerateTransactionAsync(tokenContractAddress, "Initialize", "ELF", "ELF Token",
                    totalSupply, 2U);

            await tester.MineAsync(new List<Transaction> {txToInitialize});

            var bytes = await tester.CallContractMethodAsync(tokenContractAddress, "BalanceOf",
                tester.GetCallOwnerAddress());

            var result = bytes.DeserializeToUInt64();

            Assert.Equal(totalSupply, result);
        }

        [Fact]
        public async Task GetTransactionResultTest()
        {
            var tester = new ContractTester<ContractTestAElfModule>();
            await tester.InitialChainAsync();

            var zeroContractAddress = tester.GetContractAddress(Hash.Empty);
            var tx = await tester.GenerateTransactionAsync(zeroContractAddress, "DeploySmartContract", 2,
                File.ReadAllBytes(typeof(TokenContract).Assembly.Location));

            await tester.MineAsync(new List<Transaction> {tx});

            var txResult = await tester.GetTransactionResultAsync(tx.GetHash());

            Assert.Equal(TransactionResultStatus.Mined, txResult.Status);
        }

        [Fact]
        public async Task CreateContractTesterTest()
        {
            var tester = new ContractTester<ContractTestAElfModule>();
            await tester.InitialChainAsync();
            var zeroContractAddress = tester.GetContractAddress(Hash.Empty);
            var tx = await tester.GenerateTransactionAsync(zeroContractAddress, "DeploySmartContract", 2,
                File.ReadAllBytes(typeof(TokenContract).Assembly.Location));

            await tester.MineAsync(new List<Transaction> {tx});

            var newTester = tester.CreateNewContractTester(CryptoHelpers.GenerateKeyPair());

            var chain = await newTester.GetChainAsync();
        }
    }
}