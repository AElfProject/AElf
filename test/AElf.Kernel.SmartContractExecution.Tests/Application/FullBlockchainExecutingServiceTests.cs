using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public sealed class FullBlockchainExecutingServiceTests : SmartContractExecutionExecutingTestBase
    {
        private readonly FullBlockchainExecutingService _fullBlockchainExecutingService;
        private readonly IBlockchainService _blockchainService;
        private readonly KernelTestHelper _kernelTestHelper;

        public FullBlockchainExecutingServiceTests()
        {
            _fullBlockchainExecutingService = GetRequiredService<FullBlockchainExecutingService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
        }

        [Fact]
        public async Task ExecuteBlocks_Success()
        {
            var chain = await _blockchainService.GetChainAsync();

            var previousHash = chain.BestChainHash;
            var previousHeight = chain.BestChainHeight;

            var blockList = new List<Block>();
            for (var i = 0; i < 3; i++)
            {
                var transactions = new List<Transaction> {_kernelTestHelper.GenerateTransaction() };
                var lastBlock = _kernelTestHelper.GenerateBlock(previousHeight, previousHash, transactions);
            
                await _blockchainService.AddBlockAsync(lastBlock);
                await _blockchainService.AddTransactionsAsync(transactions);
            
                await _blockchainService.AttachBlockToChainAsync(chain, lastBlock);
                blockList.Add(lastBlock);
                previousHash = lastBlock.GetHash();
                previousHeight = lastBlock.Height;
            }
            
            var executionResult =
                await _fullBlockchainExecutingService.ExecuteBlocksAsync(blockList);

            executionResult.ExecutedSuccessBlocks.Count.ShouldBe(blockList.Count());
            for (var i = 0; i < 3; i++)
            {
                executionResult.ExecutedSuccessBlocks[i].GetHash().ShouldBe(blockList[i].GetHash());
            }
            executionResult.ExecutedFailedBlocks.Count.ShouldBe(0);
        }
    }
}