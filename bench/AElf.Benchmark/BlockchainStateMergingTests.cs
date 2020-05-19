using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.OS;
using AElf.Types;
using BenchmarkDotNet.Attributes;

namespace AElf.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    public class BlockchainStateMergingTests : BenchmarkTestBase
    {
        private IStateStore<ChainStateInfo> _chainStateInfoCollection;
        private IBlockchainStore<Chain> _chains;
        private IBlockManager _blockManager;
        private ITransactionManager _transactionManager;
        private IBlockchainStateService _blockchainStateService;
        private IBlockStateSetManger _blockStateSetManger;
        private IBlockchainService _blockchainService;
        private IChainManager _chainManager;
        private ITxHub _txHub;
        private IBlockchainStore<TransactionResult> _transactionResultStore;
        private OSTestHelper _osTestHelper;

        private Chain _chain;
        private ChainStateInfo _chainStateInfo;
        private List<BlockStateSet> _blockStateSets;
        private List<Block> _blocks;

        [Params(1, 10, 50)] public int BlockCount;

        [Params(1, 10, 100, 1000, 3000, 5000)] public int TransactionCount;

        [GlobalSetup]
        public async Task GlobalSetup()
        {
            _chains = GetRequiredService<IBlockchainStore<Chain>>();
            _chainStateInfoCollection = GetRequiredService<IStateStore<ChainStateInfo>>();
            _blockchainStateService = GetRequiredService<IBlockchainStateService>();
            _blockStateSetManger = GetRequiredService<IBlockStateSetManger>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
            _chainManager = GetRequiredService<IChainManager>();
            _blockManager = GetRequiredService<IBlockManager>();
            _transactionManager = GetRequiredService<ITransactionManager>();
            _transactionResultStore = GetRequiredService<IBlockchainStore<TransactionResult>>();
            _txHub = GetRequiredService<ITxHub>();
            

            _blockStateSets = new List<BlockStateSet>();
            _blocks = new List<Block>();

            _chain = await _blockchainService.GetChainAsync();

            var blockHash = _chain.BestChainHash;
            while (true)
            {
                var blockState = await _blockStateSetManger.GetBlockStateSetAsync(blockHash);
                _blockStateSets.Add(blockState);

                var blockHeader = await _blockchainService.GetBlockHeaderByHashAsync(blockHash);
                blockHash = blockHeader.PreviousBlockHash;
                if (blockHash == _chain.LastIrreversibleBlockHash)
                {
                    break;
                }
            }

            await _blockchainStateService.MergeBlockStateAsync(_chain.BestChainHeight, _chain.BestChainHash);

            for (var i = 0; i < BlockCount; i++)
            {
                var transactions = await _osTestHelper.GenerateTransferTransactions(TransactionCount);
                await _osTestHelper.BroadcastTransactions(transactions);
                var block = await _osTestHelper.MinedOneBlock();
                _blocks.Add(block);

                var blockState = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
                _blockStateSets.Add(blockState);
            }

            var chain = await _blockchainService.GetChainAsync();
            await _chainManager.SetIrreversibleBlockAsync(chain, chain.BestChainHash);

            _chainStateInfo = await _chainStateInfoCollection.GetAsync(chain.Id.ToStorageKey());
        }

        [Benchmark]
        public async Task MergeBlockStateTest()
        {
            var chain = await _blockchainService.GetChainAsync();
            await _blockchainStateService.MergeBlockStateAsync(chain.BestChainHeight, chain.BestChainHash);
        }

        [IterationCleanup]
        public async Task IterationCleanup()
        {
            await _chainStateInfoCollection.SetAsync(_chain.Id.ToStorageKey(), _chainStateInfo);
            foreach (var blockStateSet in _blockStateSets)
            {
                await _blockStateSetManger.SetBlockStateSetAsync(blockStateSet);
            }
        }

        [GlobalCleanup]
        public async Task GlobalCleanup()
        {
            foreach (var block in _blocks)
            {
                await _txHub.HandleBlockAcceptedAsync(new BlockAcceptedEvent
                {
                    BlockExecutedSet = new BlockExecutedSet() {Block = block}
                });

                await _transactionManager.RemoveTransactionsAsync(block.Body.TransactionIds);
                await _transactionResultStore.RemoveAllAsync(block.Body.TransactionIds
                    .Select(t => HashHelper.XorAndCompute(t, block.GetHash()).ToStorageKey())
                    .ToList());
                await _chainManager.RemoveChainBlockLinkAsync(block.GetHash());
                await _blockManager.RemoveBlockAsync(block.GetHash());
            }

            await _txHub.HandleBestChainFoundAsync(new BestChainFoundEventData
            {
                BlockHash = _chain.BestChainHash,
                BlockHeight = _chain.BestChainHeight
            });

            await _chains.SetAsync(_chain.Id.ToStorageKey(), _chain);
        }
    }
}