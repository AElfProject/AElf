using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Genesis;
using AElf.Contracts.Token;
using AElf.Cryptography;
using AElf.Kernel;
using Xunit;

namespace AElf.Contracts.TestBase.Tests
{
    public class ContractTesterTest
    {
        private int ChainId { get; } = ChainHelpers.ConvertBase58ToChainId("AELF");

        [Fact]
        public async Task InitialChainTest_ContractsDeployment_Test()
        {
            var tester = new ContractTester(ChainId);
            var addresses = await tester.InitialChainAsync(typeof(BasicContractZero));

            var chain = await tester.GetChainAsync();
            var longestChainHeight = chain.LongestChainHeight;
            
            Assert.Equal(1UL, longestChainHeight);
            // One contract deployed.
            Assert.Single(addresses);
        }

        [Fact]
        public async Task MineABlockTest()
        {
            var tester = new ContractTester(ChainId);
            var addresses = await tester.InitialChainAsync(typeof(BasicContractZero));

            // Create a transaction to deploy token contract.
            var tx = tester.GenerateTransaction(addresses[0], "DeploySmartContract",
                CryptoHelpers.GenerateKeyPair(), 2,
                File.ReadAllBytes(typeof(TokenContract).Assembly.Location));
            
            var chain = await tester.GetChainAsync();
            var longestChainHeight = chain.LongestChainHeight;
            
            Assert.Equal(1UL, longestChainHeight);

            await tester.MineABlockAsync(new List<Transaction> {tx});

            chain = await tester.GetChainAsync();
            longestChainHeight = chain.LongestChainHeight;

            // The longest chain height increased from 1 to 2.
            Assert.Equal(2UL, longestChainHeight);
        }

        [Fact]
        public async Task MultipleNodesTest()
        {
            var tester1 = new ContractTester(ChainId);
            var addresses = await tester1.InitialChainAsync(typeof(BasicContractZero));
            
            var tester2 = new ContractTester(ChainId);
            await tester2.InitialChainAsync(typeof(BasicContractZero));

            var tx = tester1.GenerateTransaction(addresses[0], "DeploySmartContract",
                CryptoHelpers.GenerateKeyPair(), 2,
                File.ReadAllBytes(typeof(TokenContract).Assembly.Location));
            
            await tester1.MineABlockAsync(new List<Transaction> {tx});
            
            var chain1 = await tester1.GetChainAsync();
            var chain2 = await tester2.GetChainAsync();
            
            // For different nodes, the chain information are different.
            Assert.Equal(2UL, chain1.BestChainHeight);
            Assert.Equal(1UL, chain2.BestChainHeight);
        }

        [Fact]
        public async Task SetLibTest()
        {
            var tester = new ContractTester(ChainId);
            await tester.InitialChainAsync(typeof(BasicContractZero));

            await tester.SetIrreversibleBlock(1UL);
            var chain = await tester.GetChainAsync();
            
            Assert.Equal(1UL, chain.LastIrreversibleBlockHeight);
        }
    }
}