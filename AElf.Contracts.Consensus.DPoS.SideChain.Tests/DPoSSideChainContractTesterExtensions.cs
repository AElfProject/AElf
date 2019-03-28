using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Consensus.DPoS;
using AElf.Contracts.Consensus.DPoS.SideChain;
using AElf.Contracts.TestBase;
using AElf.Kernel;
using AElf.Kernel.Consensus.Application;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Contracts.DPoS.SideChain
{
    public static class DPoSSideChainContractTesterExtensions
    {
        public static async Task<ConsensusCommand> GetConsensusCommandAsync(
            this ContractTester<DPoSSideChainTestAElfModule> tester,
            Timestamp timestamp = null)
        {
            var triggerInformation = new DPoSTriggerInformation
            {
                Timestamp = timestamp ?? DateTime.UtcNow.ToTimestamp(),
                PublicKey = tester.KeyPair.PublicKey.ToHex(),
                IsBootMiner = true,
            };
            var bytes = await tester.CallContractMethodAsync(
                tester.GetConsensusContractAddress(), // Usually the second contract is consensus contract.
                ConsensusConsts.GetConsensusCommand,
                triggerInformation);
            return ConsensusCommand.Parser.ParseFrom(bytes);
        }
        
        public static async Task<DPoSInformation> GetNewConsensusInformationAsync(
            this ContractTester<DPoSSideChainTestAElfModule> tester,
            DPoSTriggerInformation triggerInformation)
        {
            var bytes = await tester.CallContractMethodAsync(tester.GetConsensusContractAddress(),
                ConsensusConsts.GetNewConsensusInformation, triggerInformation);
            return DPoSInformation.Parser.ParseFrom(bytes);
        }
        
        public static async Task<List<Transaction>> GenerateConsensusTransactionsAsync(
            this ContractTester<DPoSSideChainTestAElfModule> tester,
            DPoSTriggerInformation triggerInformation)
        {
            var bytes = await tester.CallContractMethodAsync(tester.GetConsensusContractAddress(),
                ConsensusConsts.GenerateConsensusTransactions, triggerInformation);
            var txs = TransactionList.Parser.ParseFrom(bytes).Transactions.ToList();
            tester.SignTransaction(ref txs, tester.KeyPair);
            tester.SupplyTransactionParameters(ref txs);

            return txs;
        }
        
        public static async Task<Block> GenerateConsensusTransactionsAndMineABlockAsync(
            this ContractTester<DPoSSideChainTestAElfModule> tester,
            DPoSTriggerInformation triggerInformation,
            params ContractTester<DPoSSideChainTestAElfModule>[] testersGonnaExecuteThisBlock)
        {
            var bytes = await tester.CallContractMethodAsync(tester.GetConsensusContractAddress(),
                ConsensusConsts.GenerateConsensusTransactions,
                triggerInformation);
            var txs = TransactionList.Parser.ParseFrom(bytes).Transactions.ToList();
            tester.SignTransaction(ref txs, tester.KeyPair);
            tester.SupplyTransactionParameters(ref txs);

            var block = await tester.MineAsync(txs);
            foreach (var contractTester in testersGonnaExecuteThisBlock)
            {
                await contractTester.ExecuteBlock(block, txs);
            }

            return block;
        }
        
        public static async Task<ValidationResult> ValidateConsensusBeforeExecutionAsync(
            this ContractTester<DPoSSideChainTestAElfModule> tester,
            DPoSInformation information)
        {
            var bytes = await tester.CallContractMethodAsync(tester.GetConsensusContractAddress(),
                ConsensusConsts.ValidateConsensusBeforeExecution, information);
            return ValidationResult.Parser.ParseFrom(bytes);
        }
        
        public static async Task<ValidationResult> ValidateConsensusAfterExecutionAsync(
            this ContractTester<DPoSSideChainTestAElfModule> tester,
            DPoSInformation information)
        {
            var bytes = await tester.CallContractMethodAsync(tester.GetConsensusContractAddress(),
                ConsensusConsts.ValidateConsensusAfterExecution, information);
            return ValidationResult.Parser.ParseFrom(bytes);
        }
        
        public static async Task<TransactionResult> ExecuteConsensusContractMethodWithMiningAsync(
            this ContractTester<DPoSSideChainTestAElfModule> contractTester, string methodName, IMessage input)
        {
            return await contractTester.ExecuteContractWithMiningAsync(contractTester.GetConsensusContractAddress(),
                methodName, input);
        }

        public static async Task<Round> GetCurrentRoundInformation(
            this ContractTester<DPoSSideChainTestAElfModule> contractTester
        )
        {
            var result = await contractTester.ExecuteConsensusContractMethodWithMiningAsync(nameof(ConsensusContract.GetCurrentRoundInformation
                ), new Empty());
            return Round.Parser.ParseFrom(result.ReturnValue);
        }
    }
}