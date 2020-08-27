using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Types;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public sealed class FullBlockchainExecutingServiceValidateBeforeFailedTests : ValidateBeforeFailedTestBase
    {
        private readonly FullBlockchainExecutingService _fullBlockchainExecutingService;
        private readonly IBlockchainService _blockchainService;
        private readonly IChainManager _chainManager;
        private readonly KernelTestHelper _kernelTestHelper;

        public FullBlockchainExecutingServiceValidateBeforeFailedTests()
        {
            _fullBlockchainExecutingService = GetRequiredService<FullBlockchainExecutingService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _chainManager = GetRequiredService<IChainManager>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
        }

        [Fact]
        public async Task ExecuteBlock_ValidateFailed()
        {
            var chain = await _blockchainService.GetChainAsync();

            var previousHash = chain.BestChainHash;
            var previousHeight = chain.BestChainHeight;
            var blockList = new List<Block>();
            for (var i = 0; i < 3; i++)
            {
                var transactions = new List<Transaction> {_kernelTestHelper.GenerateTransaction()};
                var lastBlock = _kernelTestHelper.GenerateBlock(previousHeight, previousHash, transactions);

                await _blockchainService.AddBlockAsync(lastBlock);
                await _blockchainService.AddTransactionsAsync(transactions);

                await _blockchainService.AttachBlockToChainAsync(chain, lastBlock);
                previousHash = lastBlock.GetHash();
                previousHeight = lastBlock.Height;
                blockList.Add(lastBlock);
            }

            var executionResult =
                await _fullBlockchainExecutingService.ExecuteBlocksAsync(blockList);

            executionResult.SuccessBlockExecutedSets.Count.ShouldBe(0);
            executionResult.ExecutedFailedBlocks.Count.ShouldBe(1);
            executionResult.ExecutedFailedBlocks[0].GetHash().ShouldBe(blockList[0].GetHash());
        }
    }
}