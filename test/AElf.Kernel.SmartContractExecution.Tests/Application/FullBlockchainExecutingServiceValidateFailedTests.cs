using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Types;
using AElf.Kernel.Infrastructure;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public class FullBlockchainExecutingServiceValidateFailedTests : ValidateAfterFailedTestBase
    {
        private readonly FullBlockchainExecutingService _fullBlockchainExecutingService;
        private readonly IBlockchainService _blockchainService;
        private readonly IChainManager _chainManager;
        private readonly KernelTestHelper _kernelTestHelper;
        
        public FullBlockchainExecutingServiceValidateFailedTests()
        {
            _fullBlockchainExecutingService = GetRequiredService<FullBlockchainExecutingService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _chainManager = GetRequiredService<IChainManager>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
        }
        
        [Fact]
        public async Task ExecuteBlocksAttachedToLongestChain_ValidateFailed()
        {
            var chain = await _blockchainService.GetChainAsync();
            var bestChainHeight = chain.BestChainHeight;
            var bestChainHash = chain.BestChainHash;
            
            var previousHash = chain.BestChainHash;
            var previousHeight = chain.BestChainHeight;

            BlockAttachOperationStatus status = BlockAttachOperationStatus.None;
            var blockList = new List<Block>();
//            Block lastBlock = null;
            int count = 0;
            while (!status.HasFlag(BlockAttachOperationStatus.LongestChainFound))
            {
                var transactions = new List<Transaction> {_kernelTestHelper.GenerateTransaction() };
                var lastBlock = _kernelTestHelper.GenerateBlock(previousHeight, previousHash, transactions);
            
                await _blockchainService.AddBlockAsync(lastBlock);
                await _blockchainService.AddTransactionsAsync(transactions);
            
                status = await _blockchainService.AttachBlockToChainAsync(chain, lastBlock);
                count++;
                previousHash = lastBlock.GetHash();
                previousHeight = lastBlock.Height;
                blockList.Add(lastBlock);
            }
            
            var attachResult =
                await _fullBlockchainExecutingService.ExecuteBlocksAttachedToLongestChain(chain, status);
            
            attachResult.ShouldBeNull();

            chain = await _blockchainService.GetChainAsync();
            var newBlockLink = await _chainManager.GetChainBlockLinkAsync(blockList.First().GetHash());
            
            newBlockLink.ExecutionStatus.ShouldBe(ChainBlockLinkExecutionStatus.ExecutionFailed);
            chain.BestChainHash.ShouldBe(bestChainHash);
            chain.BestChainHeight.ShouldBe(bestChainHeight);
            chain.LongestChainHash.ShouldBe(bestChainHash);
            chain.LongestChainHeight.ShouldBe(bestChainHeight);
            chain.Branches.ShouldNotContainKey(previousHash.ToStorageKey());
        }
    }
}