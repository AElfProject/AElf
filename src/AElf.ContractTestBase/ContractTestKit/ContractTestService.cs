using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.CSharp.Core;
using AElf.Kernel;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Miner;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Types;
using Google.Protobuf.WellKnownTypes;

namespace AElf.ContractTestBase.ContractTestKit
{
    public interface IContractTestService
    {
        T GetTester<T>(Address contractAddress, ECKeyPair senderKey) where T : ContractStubBase, new();
        Task<BlockExecutedSet> MineAsync(List<Transaction> txs, Timestamp blockTime = null);
        Task<TransactionResult> ExecuteTransactionWithMiningAsync(Transaction transaction, Timestamp blockTime = null);

    }
    
    public class ContractTestService : IContractTestService
    {
        private readonly IContractTesterFactory _contractTesterFactory;
        private readonly IBlockchainService _blockchainService;
        private readonly IMiningService _miningService;
        private readonly IBlockAttachService _blockAttachService;

        public ContractTestService(IContractTesterFactory contractTesterFactory, IBlockchainService blockchainService,
            IMiningService miningService, IBlockAttachService blockAttachService)
        {
            _contractTesterFactory = contractTesterFactory;
            _blockchainService = blockchainService;
            _miningService = miningService;
            _blockAttachService = blockAttachService;
        }

        public T GetTester<T>(Address contractAddress, ECKeyPair senderKey) where T : ContractStubBase, new()
        {
            return _contractTesterFactory.Create<T>(contractAddress, senderKey);
        }
        
        /// <summary>
        /// Mine a block with given normal txs and system txs.
        /// Normal txs will use tx pool while system txs not.
        /// </summary>
        /// <param name="txs"></param>
        /// <param name="blockTime"></param>
        /// <returns></returns>
        public async Task<BlockExecutedSet> MineAsync(List<Transaction> txs, Timestamp blockTime = null)
        {
            var preBlock = await _blockchainService.GetBestChainLastBlockHeaderAsync();
            return await MineAsync(txs, blockTime, preBlock.GetHash(), preBlock.Height);
        }
        
        public async Task<TransactionResult> ExecuteTransactionWithMiningAsync(Transaction transaction, Timestamp blockTime = null)
        {
            var blockExecutedSet = await MineAsync(new List<Transaction> {transaction}, blockTime);
            var result = blockExecutedSet.TransactionResultMap[transaction.GetHash()];

            return result;
        }
        
        private async Task<BlockExecutedSet> MineAsync(List<Transaction> txs, Timestamp blockTime, Hash preBlockHash,
            long preBlockHeight)
        {
            var blockExecutedSet = await _miningService.MineAsync(
                new RequestMiningDto
                {
                    PreviousBlockHash = preBlockHash, PreviousBlockHeight = preBlockHeight,
                    BlockExecutionTime = TimestampHelper.DurationFromMilliseconds(int.MaxValue),
                    TransactionCountLimit = Int32.MaxValue
                }, txs, blockTime ?? DateTime.UtcNow.ToTimestamp());
            
            var block = blockExecutedSet.Block;

            await _blockchainService.AddTransactionsAsync(txs);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            return blockExecutedSet;
        }
    }
}