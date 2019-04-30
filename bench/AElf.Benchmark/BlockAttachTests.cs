using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS;
using BenchmarkDotNet.Attributes;

namespace AElf.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    public class BlockAttachTests : BenchmarkTestBase
    {
        private IBlockchainStore<Chain> _chains;
        private INotModifiedCachedStateStore<BlockStateSet> _blockStateSets;
        private IBlockManager _blockManager;
        private IChainManager _chainManager;
        private ITransactionResultManager _transactionResultManager;
        private IBlockchainService _blockchainService;
        private IBlockAttachService _blockAttachService;
        private ITransactionManager _transactionManager;
        private OSTestHelper _osTestHelper;

        private Chain _chain;
        private Block _block;
                
        [Params(1, 10, 100, 1000, 3000, 5000)]
        public int TransactionCount;

        [GlobalSetup]
        public async Task GlobalSetup()
        {
            _chains = GetRequiredService<IBlockchainStore<Chain>>();
            _blockStateSets = GetRequiredService<INotModifiedCachedStateStore<BlockStateSet>>();
            _chainManager = GetRequiredService<IChainManager>();
            _blockManager = GetRequiredService<IBlockManager>();
            _transactionResultManager = GetRequiredService<ITransactionResultManager>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _blockAttachService = GetRequiredService<IBlockAttachService>();
            _transactionManager = GetRequiredService<ITransactionManager>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
            
            _chain = await _blockchainService.GetChainAsync();
        }

        [IterationSetup]
        public async Task IterationSetup()
        {
            var transactions = await _osTestHelper.GenerateTransferTransactions(TransactionCount);
            _block = _osTestHelper.GenerateBlock(_chain.BestChainHash, _chain.BestChainHeight, transactions);
        }

        [Benchmark]
        public async Task AttachBlockTest()
        {
            await _blockAttachService.AttachBlockAsync(_block);
        }

        [IterationCleanup]
        public async Task IterationCleanup()
        {
            await _blockStateSets.RemoveAsync(_block.GetHash().ToStorageKey());
            foreach (var tx in _block.Body.Transactions)
            {
                _transactionManager.RemoveTransaction(tx);
                _transactionResultManager.RemoveTransactionResultAsync(tx, _block.GetHash());
                _transactionResultManager.RemoveTransactionResultAsync(tx,_block.Header.GetPreMiningHash());
            }
            await _chainManager.RemoveChainBlockLinkAsync(_block.GetHash());
            await _blockManager.RemoveBlockAsync(_block.GetHash());
            await _chains.SetAsync(_chain.Id.ToStorageKey(), _chain);
        }
    }
}