using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS;
using AElf.Types;
using BenchmarkDotNet.Attributes;

namespace AElf.Benchmark
{
   [MarkdownExporterAttribute.GitHub]
    public class BlockExecutingParalleGroupslTests : BenchmarkTestBase
    {
        private IBlockExecutingService _blockExecutingService;
        private IBlockchainService _blockchainService;
        private IMinerService _minerService;
        private ITransactionResultManager _transactionResultManager;
        private INotModifiedCachedStateStore<BlockStateSet> _blockStateSets;
        private OSTestHelper _osTestHelper;

        private List<Transaction> _prepareTransactions;
        private List<Transaction> _transactions;
        private List<ECKeyPair> _keyPairs;
        private Block _block;

        
        [Params(1, 10, 20, 50, 100)] 
        public int GroupCount;

        public int TransactionCount = 2000;

        [GlobalSetup]
        public async Task GlobalSetup()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _blockExecutingService = GetRequiredService<IBlockExecutingService>();
            _minerService = GetRequiredService<IMinerService>();
            _transactionResultManager = GetRequiredService<ITransactionResultManager>();
            _blockStateSets = GetRequiredService<INotModifiedCachedStateStore<BlockStateSet>>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
            
            _prepareTransactions = new List<Transaction>();
            _transactions = new List<Transaction>();
            _keyPairs = new List<ECKeyPair>();
        }

        [IterationSetup]
        public async Task IterationSetup()
        {
            var chain = await _blockchainService.GetChainAsync();

            _block = new Block
            {
                Header = new BlockHeader
                {
                    ChainId = chain.Id,
                    Height = chain.BestChainHeight + 1,
                    PreviousBlockHash = chain.BestChainHash,
                    Time = TimestampHelper.GetUtcNow()
                },
                Body = new BlockBody()
            };
            var tokenAmount = TransactionCount / GroupCount;
            (_prepareTransactions, _keyPairs) = await _osTestHelper.PrepareTokenForParallel(GroupCount, tokenAmount);
            await _blockExecutingService.ExecuteBlockAsync(_block.Header, _prepareTransactions);
            await _osTestHelper.BroadcastTransactions(_prepareTransactions);
            _block = await _minerService.MineAsync(chain.BestChainHash, chain.BestChainHeight,
                TimestampHelper.GetUtcNow(), TimestampHelper.DurationFromSeconds(4));
            
            _transactions = await _osTestHelper.GenerateTransactionsWithoutConflict(_keyPairs, tokenAmount);
        }
        
        [Benchmark]
        public async Task ExecuteBlock()
        {
            _block = await _blockExecutingService.ExecuteBlockAsync(_block.Header, _transactions);
        }

        [IterationCleanup]
        public async Task IterationCleanup()
        {
            await _blockStateSets.RemoveAsync(_block.GetHash().ToStorageKey());
            foreach (var transaction in _transactions.Concat(_prepareTransactions))
            {
                await _transactionResultManager.RemoveTransactionResultAsync(transaction.GetHash(), _block.GetHash());
                await _transactionResultManager.RemoveTransactionResultAsync(transaction.GetHash(),
                    _block.Header.GetPreMiningHash());
            }
        }
    }
}