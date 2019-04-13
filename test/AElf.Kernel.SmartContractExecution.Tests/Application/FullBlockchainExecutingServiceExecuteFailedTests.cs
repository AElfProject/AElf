using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public class FullBlockchainExecutingServiceExecuteFailedTests : ExecuteFailedTestBase
    {
        private readonly FullBlockchainExecutingService _fullBlockchainExecutingService;
        private readonly IBlockchainService _blockchainService;
        private readonly IChainManager _chainManager;
        private readonly KernelTestHelper _kernelTestHelper;

        public FullBlockchainExecutingServiceExecuteFailedTests()
        {
            _fullBlockchainExecutingService = GetRequiredService<FullBlockchainExecutingService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _chainManager = GetRequiredService<IChainManager>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
        }

        [Fact]
        public async Task ExecuteBlocksAttachedToLongestChain_ExecuteFailed()
        {
            var chain = await _blockchainService.GetChainAsync();
            var bestChainHeight = chain.BestChainHeight;
            var bestChainHash = chain.BestChainHash;

            var newBlock = _kernelTestHelper.GenerateBlock(chain.BestChainHeight, chain.BestChainHash,
                new List<Transaction>{_kernelTestHelper.GenerateTransaction()});

            await _blockchainService.AddBlockAsync(newBlock);
            var status = await _blockchainService.AttachBlockToChainAsync(chain, newBlock);
            var attachResult =
                await _fullBlockchainExecutingService.ExecuteBlocksAttachedToLongestChain(chain, status);

            attachResult.Count.ShouldBe(1);
            attachResult.Last().Height.ShouldBe(16);
            attachResult.Last().BlockHash.ShouldBe(newBlock.GetHash());

            chain = await _blockchainService.GetChainAsync();
            var newBlockLink = await _chainManager.GetChainBlockLinkAsync(newBlock.GetHash());

            newBlockLink.ExecutionStatus.ShouldBe(ChainBlockLinkExecutionStatus.ExecutionFailed);
            chain.BestChainHash.ShouldBe(bestChainHash);
            chain.BestChainHeight.ShouldBe(bestChainHeight);
        }
    }
}