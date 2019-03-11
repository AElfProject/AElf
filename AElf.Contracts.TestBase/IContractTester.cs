using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;

namespace AElf.Contracts.TestBase
{
    public interface IContractTester<TContractTestAElfModule> where TContractTestAElfModule : ContractTestAElfModule
    {
        /// <summary>
        /// Initial a chain and create genesis block which has several contracts deployed.
        /// </summary>
        /// <param name="contractTypes">Types of smart contracts gonna deployed in genesis block.</param>
        /// <returns></returns>
        Task InitialChainAsync(params Type[] contractTypes);
        
        /// <summary>
        /// When testing contract, we can only package txs we want to.
        /// </summary>
        /// <param name="transactions">Need to mock ITxHub.GetExecutableTransactionSetAsync</param>
        /// <param name="systemTransactions">Need to mock ISystemTransactionGenerationService.GenerateSystemTransactions</param>
        /// <returns></returns>
        Task<Block> MineAsync(List<Transaction> transactions, List<Transaction> systemTransactions);

        /// <summary>
        /// Because for now the txs are getting from TransactionManager when we,
        /// so the node gonna execute one block should mock TransactionManager
        /// for returning these txs.
        /// </summary>
        /// <param name="block">The block produced by another Tester instance.</param>
        /// <param name="transactions">Used to mock TransactionManager</param>
        /// <param name="systemTransactions">Used to mock TransactionManager</param>
        /// <returns></returns>
        Task ExecuteBlockAsync(Block block, List<Transaction> transactions, List<Transaction> systemTransactions);

        /// <summary>
        /// Get transaction result from ITransactionResultQueryService.
        /// </summary>
        /// <param name="txId"></param>
        /// <returns></returns>
        Task<TransactionResult> GetTransactionResult(Hash txId);
        
        
    }
}