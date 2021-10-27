using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.TransactionPool;
using AElf.Kernel.TransactionPool.Application;
using AElf.OS;
using BenchmarkDotNet.Attributes;

namespace AElf.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    public class TxHubHandleBestChainFoundTests : BenchmarkTestBase
    {
        private IBlockchainStore<Chain> _chains;
        private IBlockchainStore<ChainBlockLink> _chainBlockLinks;
        private IChainManager _chainManager;
        private IBlockManager _blockManager;
        private ITransactionManager _transactionManager;
        private ITransactionPoolService _transactionPoolService;
        private IBlockchainService _blockchainService;
        private OSTestHelper _osTestHelper;

        private Chain _chain;
        private Block _block;

        [Params(1, 10, 100, 1000, 3000, 5000)] public int TransactionCount;

        [GlobalSetup]
        public async Task GlobalSetup()
        {
            _chains = GetRequiredService<IBlockchainStore<Chain>>();
            _chainBlockLinks = GetRequiredService<IBlockchainStore<ChainBlockLink>>();
            _chainManager = GetRequiredService<IChainManager>();
            _blockManager = GetRequiredService<IBlockManager>();
            _transactionManager = GetRequiredService<ITransactionManager>();
            _transactionPoolService = GetRequiredService<ITransactionPoolService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _osTestHelper = GetRequiredService<OSTestHelper>();

            _chain = await _blockchainService.GetChainAsync();
        }

        [IterationSetup]
        public async Task IterationSetup()
        {
            var transactions = await _osTestHelper.GenerateTransferTransactions(TransactionCount);
            await _transactionPoolService.AddTransactionsAsync(transactions);
            var chain = await _blockchainService.GetChainAsync();
            _block = _osTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
            await _blockchainService.AddBlockAsync(_block);
            await _chainBlockLinks.SetAsync(
                chain.Id.ToStorageKey() + KernelConstants.StorageKeySeparator + _block.GetHash().ToStorageKey(),
                new ChainBlockLink()
                {
                    BlockHash = _block.GetHash(),
                    Height = _block.Height,
                    PreviousBlockHash = _block.Header.PreviousBlockHash,
                    IsLinked = true
                });
            await _blockchainService.SetBestChainAsync(chain, _block.Height, _block.GetHash());
        }

        [Benchmark]
        public async Task HandleBestChainFoundTest()
        {
            await _transactionPoolService.UpdateTransactionPoolByBestChainAsync(_block.GetHash(), _block.Height);
        }

        [IterationCleanup]
        public async Task IterationCleanup()
        {
            await _transactionPoolService.CleanByTransactionIdsAsync(_block.TransactionIds);
            await _transactionPoolService.UpdateTransactionPoolByBestChainAsync(_chain.BestChainHash,
                _chain.BestChainHeight);

            await _transactionManager.RemoveTransactionsAsync(_block.Body.TransactionIds);
            await _chainManager.RemoveChainBlockLinkAsync(_block.GetHash());
            await _blockManager.RemoveBlockAsync(_block.GetHash());
            await _chains.SetAsync(_chain.Id.ToStorageKey(), _chain);
        }
    }
}