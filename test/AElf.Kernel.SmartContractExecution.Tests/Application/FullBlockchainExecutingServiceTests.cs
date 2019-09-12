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

            var previousHash = chain.BestChainHash;
            var previousHeight = chain.BestChainHeight;

            BlockAttachOperationStatus status = BlockAttachOperationStatus.None;
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
            }
            
            var attachResult =
                await _fullBlockchainExecutingService.ExecuteBlocksAttachedToLongestChain(chain, status);

            attachResult.Count.ShouldBe(count);
            attachResult.Last().Height.ShouldBe(previousHeight);

            chain = await _blockchainService.GetChainAsync();
            chain.BestChainHash.ShouldBe(previousHash);
            chain.BestChainHeight.ShouldBe(previousHeight);
        }
    }
}