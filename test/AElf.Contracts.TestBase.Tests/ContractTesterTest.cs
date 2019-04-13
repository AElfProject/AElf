using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.Consensus.DPoS;
using AElf.Contracts.Genesis;
using AElf.Contracts.MultiToken;
using AElf.Contracts.MultiToken.Messages;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Consensus;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using AElf.Types.CSharp;
using Google.Protobuf;
using Xunit;

namespace AElf.Contracts.TestBase.Tests
{
    public class ContractTesterTest
    {
        private int ChainId { get; } = ChainHelpers.ConvertBase58ToChainId("AELF");
        private int DefaultCategory { get; } = SmartContractTestConstants.TestRunnerCategory;

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
            var tx = await tester.GenerateTransactionAsync(zeroContractAddress,
                "DeploySmartContract", 
                new ContractDeploymentInput()
                {
                    Category = SmartContractTestConstants.TestRunnerCategory,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(TokenContract).Assembly.Location))
                });

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

            var tx = await tester1.GenerateTransactionAsync(zeroContractAddress, 
                "DeploySmartContract", 
                new ContractDeploymentInput()
                {
                    Category = SmartContractTestConstants.TestRunnerCategory,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(TokenContract).Assembly.Location))
                });

            await tester1.MineAsync(new List<Transaction> {tx});

            var chain1 = await tester1.GetChainAsync();
            var chain2 = await tester2.GetChainAsync();

            // For different nodes, the chain information are different.
            Assert.Equal(2, chain1.BestChainHeight);
            Assert.Equal(1, chain2.BestChainHeight);
        }

        // TODO: Think about another way to test `CallContractMethodAsync`.
        [Fact]
        public async Task CallContractTest()
        {
            var callerKeyPair = CryptoHelpers.GenerateKeyPair();
            var tester = new ContractTester<ContractTestAElfModule>(ChainId, callerKeyPair);
            await tester.InitialChainAsync(list =>
            {
                list.AddGenesisSmartContract<ConsensusContract>(
                    ConsensusSmartContractAddressNameProvider.Name);
                list.AddGenesisSmartContract<TokenContract>(
                    TokenSmartContractAddressNameProvider.Name);
            });

            var tokenContractAddress = tester.GetContractAddress(TokenSmartContractAddressNameProvider.Name);

            var bytes = await tester.CallContractMethodAsync(tokenContractAddress, nameof(TokenContract.GetBalance),
                new GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = tester.GetCallOwnerAddress(),
                });

            var balanceOutput = GetBalanceOutput.Parser.ParseFrom(bytes);

            Assert.Equal(0L, balanceOutput.Balance);
        }

        [Fact]
        public async Task GetTransactionResultTest()
        {
            var tester = new ContractTester<ContractTestAElfModule>();
            await tester.InitialChainAsync();

            var zeroContractAddress = tester.GetContractAddress(Hash.Empty);
            var tx = await tester.GenerateTransactionAsync(zeroContractAddress, "DeploySmartContract",
                new ContractDeploymentInput
                {
                    Category = DefaultCategory,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(TokenContract).Assembly.Location))
                }
            );

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
            var tx = await tester.GenerateTransactionAsync(zeroContractAddress, 
                "DeploySmartContract",
                new ContractDeploymentInput()
                {
                    Category = SmartContractTestConstants.TestRunnerCategory,
                    Code = ByteString.CopyFrom(File.ReadAllBytes(typeof(TokenContract).Assembly.Location))
                });

            await tester.MineAsync(new List<Transaction> {tx});

            var newTester = tester.CreateNewContractTester(CryptoHelpers.GenerateKeyPair());
            var chain = await newTester.GetChainAsync();

            Assert.Equal(2L, chain.BestChainHeight);
        }
    }
}