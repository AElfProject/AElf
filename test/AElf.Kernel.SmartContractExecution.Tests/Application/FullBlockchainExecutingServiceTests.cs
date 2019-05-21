using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
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
        public async Task Attach_Block_To_Chain_ReturnNull()
        {
            var chain = await _blockchainService.GetChainAsync();

            var newBlock = _kernelTestHelper.GenerateBlock(chain.BestChainHeight, Hash.Empty,
                new List<Transaction>{_kernelTestHelper.GenerateTransaction()});

            var status = await _blockchainService.AttachBlockToChainAsync(chain, newBlock);

            var attachResult =
                await _fullBlockchainExecutingService.ExecuteBlocksAttachedToLongestChain(chain, status);
            attachResult.ShouldBeNull();
        }

        [Fact]
        public async Task Attach_Block_To_Chain_FoundBestChain()
        {
            var chain = await _blockchainService.GetChainAsync();

            var transactions = new List<Transaction> { _kernelTestHelper.GenerateTransaction() };
            var newBlock = _kernelTestHelper.GenerateBlock(chain.BestChainHeight, chain.BestChainHash, transactions);
            
            await _blockchainService.AddBlockAsync(newBlock);
            await _blockchainService.AddTransactionsAsync(transactions);
            
            var status = await _blockchainService.AttachBlockToChainAsync(chain, newBlock);
            var attachResult =
                await _fullBlockchainExecutingService.ExecuteBlocksAttachedToLongestChain(chain, status);

            attachResult.Count.ShouldBe(1);
            attachResult.Last().Height.ShouldBe(newBlock.Height);

            chain = await _blockchainService.GetChainAsync();
            chain.BestChainHash.ShouldBe(newBlock.GetHash());
            chain.BestChainHeight.ShouldBe(newBlock.Height);
        }
    }
}