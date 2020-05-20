using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS;
using AElf.Types;
using BenchmarkDotNet.Attributes;

namespace AElf.Benchmark
{
    [MarkdownExporterAttribute.GitHub]
    public class BlockExecutingTests : BenchmarkTestBase
    {
        private IBlockExecutingService _blockExecutingService;
        private IBlockchainService _blockchainService;
        private INotModifiedCachedStateStore<BlockStateSet> _blockStateSets;
        private OSTestHelper _osTestHelper;

        private List<Transaction> _transactions;
        private Block _block;

        [Params(1, 10, 100, 1000, 3000, 5000)] public int TransactionCount;

        [GlobalSetup]
        public void GlobalSetup()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _blockExecutingService = GetRequiredService<IBlockExecutingService>();
            _blockStateSets = GetRequiredService<INotModifiedCachedStateStore<BlockStateSet>>();
            _osTestHelper = GetRequiredService<OSTestHelper>();
        }

        [IterationSetup]
        public async Task IterationSetup()
        {
            var chain = await _blockchainService.GetChainAsync();

            _transactions = await _osTestHelper.GenerateTransferTransactions(TransactionCount);
            _block = _osTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, _transactions);
        }

        [Benchmark]
        public async Task ExecuteBlock()
        {
            _block = (await _blockExecutingService.ExecuteBlockAsync(_block.Header, _transactions)).Block;
        }

        [IterationCleanup]
        public async Task IterationCleanup()
        {
            await _blockStateSets.RemoveAsync(_block.GetHash().ToStorageKey());
            var transactionIds = _transactions.Select(t => t.GetHash()).ToList();
            await RemoveTransactionResultsAsync(transactionIds, _block.GetHash());
        }
    }
}