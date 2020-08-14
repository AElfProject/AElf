using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.TransactionPool.Application;
using AElf.Kernel.Txn.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Miner.Application
{
    public class MinerServiceTests : KernelMiningTestBase
    {
        private readonly ITransactionPoolService _transactionPoolService;
        private readonly IBlockTransactionLimitProvider _blockTransactionLimitProvider;
        private readonly ITransactionPackingOptionProvider _transactionPackingOptionProvider;
        private readonly IMinerService _minerService;
        private readonly IBlockStateSetManger _blockStateSetManger;
        private readonly IBlockchainService _blockchainService;
        private readonly KernelTestHelper _kernelTestHelper;

        public MinerServiceTests()
        {
            _transactionPoolService = GetRequiredService<ITransactionPoolService>();
            _blockTransactionLimitProvider = GetRequiredService<IBlockTransactionLimitProvider>();
            _transactionPackingOptionProvider = GetRequiredService<ITransactionPackingOptionProvider>();
            _minerService = GetRequiredService<IMinerService>();
            _blockStateSetManger = GetRequiredService<IBlockStateSetManger>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
        }

        [Fact]
        public async Task Mine_Test()
        {
            var chain = await _blockchainService.GetChainAsync();
            var blockStateSet = new BlockStateSet
            {
                BlockHash = chain.BestChainHash,
                BlockHeight = chain.BestChainHeight,
            };
            await _blockStateSetManger.SetBlockStateSetAsync(blockStateSet);

            var transactions = _kernelTestHelper.GenerateTransactions(5, chain.BestChainHeight, chain.BestChainHash);
            await _transactionPoolService.UpdateTransactionPoolByBestChainAsync(chain.BestChainHash,
                chain.BestChainHeight);
            await _transactionPoolService.AddTransactionsAsync(transactions);
            await Task.Delay(200);
            await _transactionPoolService.UpdateTransactionPoolByBestChainAsync(chain.BestChainHash,
                chain.BestChainHeight);
            
            {
                await _transactionPackingOptionProvider.SetTransactionPackingOptionAsync(new BlockIndex
                {
                    BlockHash = chain.BestChainHash,
                    BlockHeight = chain.BestChainHeight
                }, false);
                
                var blockTime = TimestampHelper.GetUtcNow();
                var result = await _minerService.MineAsync(chain.BestChainHash, chain.BestChainHeight, blockTime,
                    TimestampHelper.DurationFromSeconds(4));
                await CheckMiningResultAsync(result, blockTime, 0);
            }
            
            {
                await _transactionPackingOptionProvider.SetTransactionPackingOptionAsync(new BlockIndex
                {
                    BlockHash = chain.BestChainHash,
                    BlockHeight = chain.BestChainHeight
                }, true);
                
                await _blockTransactionLimitProvider.SetLimitAsync(new BlockIndex
                {
                    BlockHash = chain.BestChainHash,
                    BlockHeight = chain.BestChainHeight
                }, 3);
                
                var blockTime = TimestampHelper.GetUtcNow();
                var result = await _minerService.MineAsync(chain.BestChainHash, chain.BestChainHeight, blockTime,
                    TimestampHelper.DurationFromSeconds(4));
                await CheckMiningResultAsync(result, blockTime, 2);
            }

        }
        
        private async Task CheckMiningResultAsync(BlockExecutedSet blockExecutedSet, Timestamp blockTime,
            int transactionCount)
        {
            var chain = await _blockchainService.GetChainAsync();
            
            blockExecutedSet.Block.Header.PreviousBlockHash.ShouldBe(chain.BestChainHash);
            blockExecutedSet.Block.Header.Height.ShouldBe(chain.BestChainHeight + 1);
            blockExecutedSet.Block.Header.Time.ShouldBe(blockTime);
            blockExecutedSet.Block.VerifySignature().ShouldBeTrue();
            blockExecutedSet.Block.Body.TransactionsCount.ShouldBe(1 + transactionCount);
        }
    }
}