using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
    public class BlockExecutingParalleGroupslTests : BenchmarkParallelTestBase
    {
        private IBlockExecutingService _blockExecutingService;
        private IBlockchainService _blockchainService;
        private IMinerService _minerService;
        private ITransactionResultManager _transactionResultManager;
        private INotModifiedCachedStateStore<BlockStateSet> _blockStateSets;
        private OSTestHelper _osTestHelper;

        private List<Transaction> _systemTransactions;
        private List<Transaction> _prepareTransactions;
        private List<Transaction> _cancellableTransactions;
        private List<ECKeyPair> _keyPairs;
        private Block _block;
        
        [Params(1, 2, 5, 10, 50, 100)]
        public int GroupCount;

        public int TransactionCount = 200;

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
            _systemTransactions = new List<Transaction>();
            _cancellableTransactions = new List<Transaction>();
            _keyPairs = new List<ECKeyPair>();
        }

        [IterationSetup]
        public async Task IterationSetup()
        {
            var chain = await _blockchainService.GetChainAsync();
            var tokenAmount = TransactionCount / GroupCount;
            (_prepareTransactions, _keyPairs) = await _osTestHelper.PrepareTokenForParallel(GroupCount, tokenAmount);
            _block = _osTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, _prepareTransactions);
            await _blockExecutingService.ExecuteBlockAsync(_block.Header, _prepareTransactions);
            await _osTestHelper.BroadcastTransactions(_prepareTransactions);
            _block = await _minerService.MineAsync(chain.BestChainHash, chain.BestChainHeight,
                TimestampHelper.GetUtcNow(), TimestampHelper.DurationFromSeconds(4));
            
            _systemTransactions = await _osTestHelper.GenerateTransferTransactions(1);
            _cancellableTransactions = await _osTestHelper.GenerateTransactionsWithoutConflict(_keyPairs, tokenAmount);
            chain = await _blockchainService.GetChainAsync();
            _block = _osTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight,
                _systemTransactions.Concat(_cancellableTransactions));
        }
        
        [Benchmark]
        public async Task ExecuteBlock()
        {
            _block = await _blockExecutingService.ExecuteBlockAsync(_block.Header, 
                _systemTransactions, _cancellableTransactions, CancellationToken.None);
        }

        [IterationCleanup]
        public async Task IterationCleanup()
        {
            await _blockStateSets.RemoveAsync(_block.GetHash().ToStorageKey());
            foreach (var transaction in _systemTransactions.Concat(_cancellableTransactions))
            {
                await _transactionResultManager.RemoveTransactionResultAsync(transaction.GetHash(), _block.GetHash());
                await _transactionResultManager.RemoveTransactionResultAsync(transaction.GetHash(),
                    _block.Header.GetPreMiningHash());
            }
        }
    }
}