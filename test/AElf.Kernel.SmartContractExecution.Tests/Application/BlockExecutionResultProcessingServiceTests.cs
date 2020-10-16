using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public sealed class BlockExecutionResultProcessingServiceTests : SmartContractExecutionExecutingTestBase
    {
        private readonly IBlockExecutionResultProcessingService _blockExecutionResultProcessingService;
        private readonly IBlockchainService _blockchainService;
        private readonly KernelTestHelper _kernelTestHelper;
        private readonly IChainManager _chainManager;
        private readonly IChainBlockLinkService _chainBlockLinkService;

        public BlockExecutionResultProcessingServiceTests()
        {
            _blockExecutionResultProcessingService = GetRequiredService<IBlockExecutionResultProcessingService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
            _chainManager = GetRequiredService<IChainManager>();
            _chainBlockLinkService = GetRequiredService<IChainBlockLinkService>();
        }

        [Fact]
        public async Task Process_LessThanBestChainHeight_BlockExecutionResult()
        {
            var chain = await _blockchainService.GetChainAsync();
            await _blockchainService.RemoveLongestBranchAsync(chain);

            var block = _kernelTestHelper.GenerateBlock(1, Hash.Empty);
            await _blockchainService.AttachBlockToChainAsync(chain, block);

            var executionResult = new BlockExecutionResult
            {
                SuccessBlockExecutedSets = {new BlockExecutedSet {Block = block}}
            };
            await _blockExecutionResultProcessingService.ProcessBlockExecutionResultAsync(chain, executionResult);
            
            chain = await _blockchainService.GetChainAsync();
            chain.LongestChainHash.ShouldBe(chain.BestChainHash);
            chain.LongestChainHeight.ShouldBe(chain.BestChainHeight);
        }
        
        [Fact]
        public async Task Process_Success_BlockExecutionResult()
        {
            var chain = await _blockchainService.GetChainAsync();
            await _blockchainService.RemoveLongestBranchAsync(chain);

            var block = _kernelTestHelper.GenerateBlock(chain.BestChainHeight, chain.BestChainHash);
            await _blockchainService.AttachBlockToChainAsync(chain, block);
            
            chain = await _blockchainService.GetChainAsync();
            var block2 = _kernelTestHelper.GenerateBlock(block.Height, block.GetHash());
            await _blockchainService.AttachBlockToChainAsync(chain, block2);

            var executionResult = new BlockExecutionResult
            {
                SuccessBlockExecutedSets = {new BlockExecutedSet {Block = block}, new BlockExecutedSet {Block = block2}}
            };
            await _blockExecutionResultProcessingService.ProcessBlockExecutionResultAsync(chain, executionResult);
            
            chain = await _blockchainService.GetChainAsync();
            chain.BestChainHeight.ShouldBe(block2.Height);
            chain.BestChainHash.ShouldBe(block2.GetHash());

            var chainBlockLink = await _chainManager.GetChainBlockLinkAsync(block.GetHash());
            chainBlockLink.ExecutionStatus.ShouldBe(ChainBlockLinkExecutionStatus.ExecutionSuccess);
            var chainBlockLink2 = await _chainManager.GetChainBlockLinkAsync(block2.GetHash());
            chainBlockLink2.ExecutionStatus.ShouldBe(ChainBlockLinkExecutionStatus.ExecutionSuccess);
        }
        
        [Fact]
        public async Task Process_Empty_BlockExecutionResult()
        {
            var chain = await _blockchainService.GetChainAsync();
            await _blockExecutionResultProcessingService.ProcessBlockExecutionResultAsync(chain, new BlockExecutionResult());

            chain = await _blockchainService.GetChainAsync();
            chain.LongestChainHash.ShouldBe(chain.BestChainHash);
            chain.LongestChainHeight.ShouldBe(chain.BestChainHeight);
        }
        
        [Fact]
        public async Task Process_Failed_BlockExecutionResult()
        {
            var chain = await _blockchainService.GetChainAsync();
            await _blockchainService.AttachBlockToChainAsync(chain, _kernelTestHelper.LongestBranchBlockList.Last());
            
            var executionResult = new BlockExecutionResult();
            executionResult.ExecutedFailedBlocks.Add(_kernelTestHelper.LongestBranchBlockList.Last());

            await _blockExecutionResultProcessingService.ProcessBlockExecutionResultAsync(chain, executionResult);

            chain = await _blockchainService.GetChainAsync();
            chain.LongestChainHash.ShouldBe(chain.BestChainHash);
            chain.LongestChainHeight.ShouldBe(chain.BestChainHeight);

            var chainBlockLink =
                await _chainManager.GetChainBlockLinkAsync(_kernelTestHelper.LongestBranchBlockList.Last().GetHash());
            chainBlockLink.ExecutionStatus.ShouldBe(ChainBlockLinkExecutionStatus.ExecutionFailed);
        }
    }
}