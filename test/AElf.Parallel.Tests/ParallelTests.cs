using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Parallel;
using AElf.Kernel.SmartContract.Parallel.Domain;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.TestBase;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Parallel.Tests
{
    public sealed class ParallelTests : AElfIntegratedTest<ParallelTestAElfModule>
    {
        private readonly IBlockExecutingService _blockExecutingService;
        private readonly IBlockchainService _blockchainService;
        private readonly IMinerService _minerService;
        private readonly ITransactionResultManager _transactionResultManager;
        private readonly INotModifiedCachedStateStore<BlockStateSet> _blockStateSets;
        private readonly ITransactionGrouper _grouper;
        private readonly ICodeRemarksManager _codeRemarksManager;
        private readonly ITxHub _txHub;
        private readonly IBlockAttachService _blockAttachService;
        private readonly ParallelTestHelper _parallelTestHelper;
        
        private int _groupCount = 10;
        private int _transactionCount = 20;

        public ParallelTests()
        {
            _blockchainService = GetRequiredService<IBlockchainService>();
            _blockExecutingService = GetRequiredService<IBlockExecutingService>();
            _minerService = GetRequiredService<IMinerService>();
            _transactionResultManager = GetRequiredService<ITransactionResultManager>();
            _blockStateSets = GetRequiredService<INotModifiedCachedStateStore<BlockStateSet>>();
            _grouper = GetRequiredService<ITransactionGrouper>();
            _codeRemarksManager = GetRequiredService<ICodeRemarksManager>();
            _txHub = GetRequiredService<ITxHub>();
            _blockAttachService = GetRequiredService<IBlockAttachService>();
            _parallelTestHelper = GetRequiredService<ParallelTestHelper>();
        }
        
        [Fact]
        public async Task TokenTransferParallelTest()
        {
            var chain = await _blockchainService.GetChainAsync();
            var tokenAmount = _transactionCount / _groupCount;
            var (prepareTransactions, keyPairs) = await _parallelTestHelper.PrepareTokenForParallel(_groupCount, tokenAmount);
            await _parallelTestHelper.BroadcastTransactions(prepareTransactions);
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, prepareTransactions);
            block = await _blockExecutingService.ExecuteBlockAsync(block.Header, prepareTransactions);
            
            var systemTransactions = await _parallelTestHelper.GenerateTransferTransactions(1);
            var cancellableTransactions = await _parallelTestHelper.GenerateTransactionsWithoutConflict(keyPairs, tokenAmount);
            var allTransaction = systemTransactions.Concat(cancellableTransactions).ToList();
            await _parallelTestHelper.BroadcastTransactions(allTransaction);

            var groupedTransactions = await _grouper.GroupAsync(new ChainContext {BlockHash = block.GetHash(), BlockHeight = block.Height},
                cancellableTransactions);
            groupedTransactions.Parallelizables.Count.ShouldBe(_groupCount);
            groupedTransactions.NonParallelizables.Count.ShouldBe(0);

            block = _parallelTestHelper.GenerateBlock(block.GetHash(), block.Height, allTransaction);
            block = await _blockExecutingService.ExecuteBlockAsync(block.Header, systemTransactions,
                cancellableTransactions, CancellationToken.None);
            
            var codeRemarks = await _codeRemarksManager.GetCodeRemarksAsync(Hash.FromRawBytes(_parallelTestHelper.TokenContractCode));
            codeRemarks.ShouldBeNull();
            
            groupedTransactions = await _grouper.GroupAsync(new ChainContext {BlockHash = block.GetHash(), BlockHeight = block.Height},
                cancellableTransactions);
            groupedTransactions.Parallelizables.Count.ShouldBe(_groupCount);
            groupedTransactions.NonParallelizables.Count.ShouldBe(0);

            block.TransactionIds.Count().ShouldBe(allTransaction.Count);
        }
        
        [Fact]
        public async Task TokenTransferFromParallelTest()
        {
            var chain = await _blockchainService.GetChainAsync();
            var tokenAmount = _transactionCount / _groupCount;
            var (prepareTransactions, keyPairs) = await _parallelTestHelper.PrepareTokenForParallel(_groupCount, tokenAmount);
            await _parallelTestHelper.BroadcastTransactions(prepareTransactions);
            var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, prepareTransactions);
            block = await _blockExecutingService.ExecuteBlockAsync(block.Header, prepareTransactions);
            
            var transactions = await _parallelTestHelper.GenerateApproveTransactions(keyPairs, tokenAmount);
            await _parallelTestHelper.BroadcastTransactions(transactions);
            block = _parallelTestHelper.GenerateBlock(block.GetHash(), block.Height, transactions);
            block = await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions);
            
            var systemTransactions = await _parallelTestHelper.GenerateTransferTransactions(1);
            var cancellableTransactions = await _parallelTestHelper.GenerateTransferFromTransactionsWithoutConflict(keyPairs, tokenAmount);
            await _parallelTestHelper.BroadcastTransactions(systemTransactions.Concat(cancellableTransactions));

            var groupedTransactions = await _grouper.GroupAsync(new ChainContext {BlockHash = block.GetHash(), BlockHeight = block.Height},
                cancellableTransactions);
            groupedTransactions.Parallelizables.Count.ShouldBe(_groupCount);
            groupedTransactions.NonParallelizables.Count.ShouldBe(0);
            
            block = _parallelTestHelper.GenerateBlock(block.GetHash(), block.Height, systemTransactions.Concat(cancellableTransactions));
            block = await _blockExecutingService.ExecuteBlockAsync(block.Header, systemTransactions,
                cancellableTransactions, CancellationToken.None);
            
            var codeRemarks = await _codeRemarksManager.GetCodeRemarksAsync(Hash.FromRawBytes(_parallelTestHelper.TokenContractCode));
            codeRemarks.ShouldBeNull();
            
            groupedTransactions = await _grouper.GroupAsync(new ChainContext {BlockHash = block.GetHash(), BlockHeight = block.Height},
                cancellableTransactions);
            groupedTransactions.Parallelizables.Count.ShouldBe(_groupCount);
            groupedTransactions.NonParallelizables.Count.ShouldBe(0);

            block.TransactionIds.Count().ShouldBe(systemTransactions.Count + cancellableTransactions.Count);
        }

        [Fact]
        public async Task WrongParallelTest()
        {
            var chain = await _blockchainService.GetChainAsync();
            var transactions =
                _parallelTestHelper.GenerateBasicFunctionWithParallelTransactions(_groupCount, _transactionCount);
            await _parallelTestHelper.BroadcastTransactions(transactions);

            var poolSize = await _txHub.GetTransactionPoolSizeAsync();
            poolSize.ShouldBe(transactions.Count);

            var groupedTransactions = await _grouper.GroupAsync(
                new ChainContext {BlockHash = chain.BestChainHash, BlockHeight = chain.BestChainHeight},
                transactions);
            groupedTransactions.Parallelizables.Count.ShouldBe(_transactionCount);
            groupedTransactions.NonParallelizables.Count.ShouldBe(0);
            
            var block = await _minerService.MineAsync(chain.BestChainHash, chain.BestChainHeight, TimestampHelper.GetUtcNow(),
                TimestampHelper.DurationFromSeconds(4));
            block.TransactionIds.Count().ShouldBe(_groupCount);
            await _blockchainService.AddBlockAsync(block);
            await _blockAttachService.AttachBlockAsync(block);

            var codeRemarks =
                await _codeRemarksManager.GetCodeRemarksAsync(
                    Hash.FromRawBytes(_parallelTestHelper.BasicFunctionWithParallelContractCode));
            codeRemarks.ShouldNotBeNull();
            codeRemarks.NonParallelizable.ShouldBeTrue();
            
            groupedTransactions = await _grouper.GroupAsync(new ChainContext {BlockHash = block.GetHash(), BlockHeight = block.Height},
                transactions);
            
            groupedTransactions.Parallelizables.Count.ShouldBe(0);
            groupedTransactions.NonParallelizables.Count.ShouldBe(_transactionCount);
            
            poolSize = await _txHub.GetTransactionPoolSizeAsync();

            poolSize.ShouldBe(transactions.Count - block.TransactionIds.Count());

            block = await _minerService.MineAsync(block.GetHash(), block.Height, TimestampHelper.GetUtcNow(),
                TimestampHelper.DurationFromSeconds(4));
            block.TransactionIds.Count().ShouldBe(poolSize);
        }
    }
}