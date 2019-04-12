using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.OS;
using BenchmarkDotNet.Attributes;

namespace AElf.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    public class BlockchainStateMergingTests : BenchmarkTestBase
    {
        private IStateStore<ChainStateInfo> _chainStateInfoCollection;
        private IBlockchainStateManager _blockchainStateManager;
        private IBlockchainStateMergingService _blockchainStateMergingService;
        private IBlockchainService _blockchainService;
        private IChainManager _chainManager;
        private OSTestHelper _osTestHelper;

        private Chain _chain;
        private ChainStateInfo _chainStateInfo;
        private List<BlockStateSet> _blockStateSets;
        
        [Params(1, 10, 50)] 
        public int BlockCount;
        
        [Params(1, 10, 100, 1000, 3000, 5000)]
        public int TransactionCount;

        [GlobalSetup]
        public async Task GlobalSetup()
        {
            _chainStateInfoCollection = GetRequiredService<IStateStore<ChainStateInfo>>();
            _blockchainStateManager = GetRequiredService<IBlockchainStateManager>();
            _blockchainStateMergingService = GetRequiredService<IBlockchainStateMergingService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
            _chainManager = GetRequiredService<IChainManager>();
            
            _blockStateSets = new List<BlockStateSet>();
            
            _chain = await _blockchainService.GetChainAsync();
            await _blockchainStateMergingService.MergeBlockStateAsync(_chain.BestChainHeight, _chain.BestChainHash);
            
            var transactionCount = TransactionCount;
            for (var i = 0; i < BlockCount; i++)
            {
                var transactions = await _osTestHelper.GenerateTransferTransactions(transactionCount);
                await _osTestHelper.BroadcastTransactions(transactions);
                var block = await _osTestHelper.MinedOneBlock();

                var blockState = await _blockchainStateManager.GetBlockStateSetAsync(block.GetHash());
                _blockStateSets.Add(blockState);
            }

            _chain = await _blockchainService.GetChainAsync();
            await _chainManager.SetIrreversibleBlockAsync(_chain, _chain.BestChainHash);

            _chainStateInfo = await _chainStateInfoCollection.GetAsync(_chain.Id.ToStorageKey());
        }

        [Benchmark]
        public async Task MergeBlockStateTest()
        {
            await _blockchainStateMergingService.MergeBlockStateAsync(_chain.BestChainHeight, _chain.BestChainHash);
        }

        [IterationCleanup]
        public async Task IterationCleanup()
        {
            await _chainStateInfoCollection.SetAsync(_chain.Id.ToStorageKey(), _chainStateInfo);
            foreach (var blockStateSet in _blockStateSets)
            {
                await _blockchainStateManager.SetBlockStateSetAsync(blockStateSet);
            }
        }

        [GlobalCleanup]
        public async Task GlobalCleanup()
        {
            
        }
    }
}