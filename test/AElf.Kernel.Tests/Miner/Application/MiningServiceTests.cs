using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AElf.Kernel.Miner.Application
{
    public class MiningServiceTests : KernelMiningTestBase
    {
        private readonly IMiningService _miningService;
        private readonly IBlockchainService _blockchainService;
        private readonly KernelTestHelper _kernelTestHelper;
        private readonly ISystemTransactionExtraDataProvider _systemTransactionExtraDataProvider;
        private readonly IAccountService _accountService;

        public MiningServiceTests()
        {
            _miningService = GetRequiredService<IMiningService>();
            _blockchainService = GetRequiredService<IBlockchainService>();
            _kernelTestHelper = GetRequiredService<KernelTestHelper>();
            _systemTransactionExtraDataProvider = GetRequiredService<ISystemTransactionExtraDataProvider>();
            _accountService = GetRequiredService<IAccountService>();
        }

        [Fact]
        public async Task Mine_Test()
        {
            var chain = await _blockchainService.GetChainAsync();
            Timestamp blockTime;
            BlockExecutedSet miningResult;
            
            {
                blockTime = TimestampHelper.GetUtcNow().AddSeconds(-4);
                miningResult = await _miningService.MineAsync(new RequestMiningDto
                    {
                        BlockExecutionTime = TimestampHelper.DurationFromSeconds(1),
                        PreviousBlockHash = chain.BestChainHash,
                        PreviousBlockHeight = chain.BestChainHeight,
                        TransactionCountLimit = 100
                    },
                    _kernelTestHelper.GenerateTransactions(10), blockTime);
                await CheckMiningResultAsync(miningResult, blockTime, 0);
            }

            {
                blockTime = TimestampHelper.GetUtcNow().AddSeconds(4);
                miningResult = await _miningService.MineAsync(new RequestMiningDto
                    {
                        BlockExecutionTime = TimestampHelper.DurationFromMilliseconds(int.MaxValue),
                        PreviousBlockHash = chain.BestChainHash,
                        PreviousBlockHeight = chain.BestChainHeight,
                        TransactionCountLimit = 100
                    },
                    _kernelTestHelper.GenerateTransactions(10), blockTime);
                await CheckMiningResultAsync(miningResult, blockTime, 10);
            }
            
            {
                blockTime = TimestampHelper.GetUtcNow();
                miningResult = await _miningService.MineAsync(new RequestMiningDto
                    {
                        BlockExecutionTime = TimestampHelper.DurationFromSeconds(4),
                        PreviousBlockHash = chain.BestChainHash,
                        PreviousBlockHeight = chain.BestChainHeight,
                        TransactionCountLimit = 100
                    },
                    _kernelTestHelper.GenerateTransactions(10), blockTime);
                await CheckMiningResultAsync(miningResult, blockTime, 10);
            }
            
            {
                blockTime = TimestampHelper.GetUtcNow();
                miningResult = await _miningService.MineAsync(new RequestMiningDto
                    {
                        BlockExecutionTime = TimestampHelper.DurationFromSeconds(4),
                        PreviousBlockHash = chain.BestChainHash,
                        PreviousBlockHeight = chain.BestChainHeight,
                        TransactionCountLimit = 5
                    },
                    _kernelTestHelper.GenerateTransactions(10), blockTime);
                await CheckMiningResultAsync(miningResult, blockTime, 4);
            }
        }

        private async Task CheckMiningResultAsync(BlockExecutedSet blockExecutedSet, Timestamp blockTime,
            int transactionCount)
        {
            var chain = await _blockchainService.GetChainAsync();
            var pubkey = await _accountService.GetPublicKeyAsync();
            
            blockExecutedSet.Block.Header.PreviousBlockHash.ShouldBe(chain.BestChainHash);
            blockExecutedSet.Block.Header.Height.ShouldBe(chain.BestChainHeight + 1);
            blockExecutedSet.Block.Header.Time.ShouldBe(blockTime);
            blockExecutedSet.Block.Header.SignerPubkey.ShouldBe(ByteString.CopyFrom(pubkey));
            blockExecutedSet.Block.VerifySignature().ShouldBeTrue();
            blockExecutedSet.Block.Body.TransactionsCount.ShouldBe(1 + transactionCount);

            _systemTransactionExtraDataProvider.TryGetSystemTransactionCount(blockExecutedSet.Block.Header,
                out var systemTransactionCount);
            systemTransactionCount.ShouldBe(1);
        }
    }
}