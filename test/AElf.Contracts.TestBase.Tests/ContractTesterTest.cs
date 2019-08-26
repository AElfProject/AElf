using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acs0;
using AElf.Contracts.Deployer;
using AElf.Contracts.MultiToken;
using AElf.Cryptography;
using AElf.Kernel.Consensus;
using AElf.Kernel.Token;
using AElf.OS.Node.Application;
using AElf.Types;
using Google.Protobuf;
using Xunit;

namespace AElf.Contracts.TestBase.Tests
{
    public class ContractTesterTest
    {
        private IReadOnlyDictionary<string, byte[]> _codes;

        public IReadOnlyDictionary<string, byte[]> Codes =>
            _codes ?? (_codes = ContractsDeployer.GetContractCodes<ContractTesterTest>());

        public byte[] ConsensusContractCode =>
            Codes.Single(kv => kv.Key.Split(",").First().Trim().EndsWith("Consensus.AEDPoS")).Value;
        public byte[] TokenContractCode => Codes.Single(kv => kv.Key.Contains("MultiToken")).Value;
        private int ChainId { get; } = ChainHelper.ConvertBase58ToChainId("AELF");
        private int DefaultCategory { get; } = SmartContractTestConstants.TestRunnerCategory;

        [Fact]
        public async Task InitialChain_Test()
        {
            var tester = new ContractTester<ContractTestAElfModule>();
            await tester.InitialChainAsync();

            var chain = await tester.GetChainAsync();
            var longestChainHeight = chain.LongestChainHeight;

            Assert.Equal(1, longestChainHeight);
        }

        [Fact]
        public async Task Mine_Test()
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
                    Code = ByteString.CopyFrom(TokenContractCode)
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
        public async Task MultipleNodes_Test()
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
                    Code = ByteString.CopyFrom(TokenContractCode)
                });

            await tester1.MineAsync(new List<Transaction> {tx});

            var chain1 = await tester1.GetChainAsync();
            var chain2 = await tester2.GetChainAsync();

            // For different nodes, the chain information are different.
            Assert.Equal(2, chain1.BestChainHeight);
            Assert.Equal(1, chain2.BestChainHeight);
        }

        [Fact]
        public async Task CallContract_Test()
        {
            var callerKeyPair = CryptoHelper.GenerateKeyPair();
            var tester = new ContractTester<ContractTestAElfModule>(ChainId, callerKeyPair);
            await tester.InitialChainAsync(list =>
            {
                list.AddGenesisSmartContract(
                    ConsensusContractCode,
                    ConsensusSmartContractAddressNameProvider.Name);
                list.AddGenesisSmartContract(
                    TokenContractCode,
                    TokenSmartContractAddressNameProvider.Name);
            });

            var tokenContractAddress = tester.GetContractAddress(TokenSmartContractAddressNameProvider.Name);

            var bytes = await tester.CallContractMethodAsync(tokenContractAddress, 
                nameof(TokenContractContainer.TokenContractStub.GetBalance),
                new GetBalanceInput
                {
                    Symbol = "ELF",
                    Owner = tester.GetCallOwnerAddress(),
                });

            var balanceOutput = GetBalanceOutput.Parser.ParseFrom(bytes);

            Assert.Equal(0L, balanceOutput.Balance);
        }

        [Fact]
        public async Task GetTransactionResult_Test()
        {
            var tester = new ContractTester<ContractTestAElfModule>();
            await tester.InitialChainAsync();

            var zeroContractAddress = tester.GetContractAddress(Hash.Empty);
            var tx = await tester.GenerateTransactionAsync(zeroContractAddress, "DeploySmartContract",
                new ContractDeploymentInput
                {
                    Category = DefaultCategory,
                    Code = ByteString.CopyFrom(TokenContractCode)
                }
            );

            await tester.MineAsync(new List<Transaction> {tx});

            var txResult = await tester.GetTransactionResultAsync(tx.GetHash());

            Assert.Equal(TransactionResultStatus.Mined, txResult.Status);
        }

        [Fact]
        public async Task CreateContractTester_Test()
        {
            var tester = new ContractTester<ContractTestAElfModule>();
            await tester.InitialChainAsync();
            var zeroContractAddress = tester.GetContractAddress(Hash.Empty);
            var tx = await tester.GenerateTransactionAsync(zeroContractAddress, 
                "DeploySmartContract",
                new ContractDeploymentInput()
                {
                    Category = SmartContractTestConstants.TestRunnerCategory,
                    Code = ByteString.CopyFrom(TokenContractCode)
                });

            await tester.MineAsync(new List<Transaction> {tx});

            var newTester = tester.CreateNewContractTester(CryptoHelper.GenerateKeyPair());
            var chain = await newTester.GetChainAsync();

            Assert.Equal(2L, chain.BestChainHeight);
        }
    }
}