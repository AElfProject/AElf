using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Contracts.TestBase;
using AElf.Kernel;
using AElf.Kernel.Consensus.Application;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.Consensus.DPoS.Tests
{
    public static class ContractTesterConsensusExtensions
    {
        public static async Task<ConsensusCommand> GetConsensusCommand(this ContractTester tester)
        {
            var bytes = await tester.CallContractMethodAsync(tester.DeployedContractsAddresses[1], // Usually the second contract is consensus contract.
                ConsensusConsts.GetConsensusCommand, tester.KeyPair,
                DateTime.UtcNow.ToTimestamp(), tester.KeyPair.PublicKey.ToHex());
            return ConsensusCommand.Parser.ParseFrom(bytes);
        }

        public static async Task<DPoSInformation> GetConsensusInformation(this ContractTester tester, DPoSExtraInformation extraInformation)
        {
            var bytes = await tester.CallContractMethodAsync(tester.DeployedContractsAddresses[1], 
                ConsensusConsts.GetNewConsensusInformation, tester.KeyPair, extraInformation.ToByteArray());
            return DPoSInformation.Parser.ParseFrom(bytes);
        }

        public static async Task<List<Transaction>> GenerateConsensusTransactions(this ContractTester tester, DPoSExtraInformation extraInformation)
        {
            var bytes = await tester.CallContractMethodAsync(tester.DeployedContractsAddresses[1], ConsensusConsts.GenerateConsensusTransactions,
                tester.KeyPair, tester.Chain.LongestChainHeight, tester.Chain.BestChainHash.Value.Take(4).ToArray(),
                extraInformation.ToByteArray());
            var txs = TransactionList.Parser.ParseFrom(bytes).Transactions.ToList();
            tester.SignTransaction(ref txs, tester.KeyPair);
            return txs;
        }
        
        public static async Task<Block> GenerateConsensusTransactionsAndMineABlock(this ContractTester tester, DPoSExtraInformation extraInformation, params ContractTester[] testers)
        {
            var bytes = await tester.CallContractMethodAsync(tester.DeployedContractsAddresses[1], ConsensusConsts.GenerateConsensusTransactions,
                tester.KeyPair, tester.Chain.LongestChainHeight, tester.Chain.BestChainHash.Value.Take(4).ToArray(),
                extraInformation.ToByteArray());
            var systemTxs = TransactionList.Parser.ParseFrom(bytes).Transactions.ToList();
            tester.SignTransaction(ref systemTxs, tester.KeyPair);

            var block = await tester.MineABlockAsync(new List<Transaction>(), systemTxs);
            foreach (var contractTester in testers)
            {
                await contractTester.AddABlockAsync(block, new List<Transaction>(), systemTxs);
            }

            return block;
        }
    }
}