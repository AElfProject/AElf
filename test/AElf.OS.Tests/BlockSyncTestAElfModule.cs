using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Modularity;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AElf.OS
{
    [DependsOn(typeof(OSTestAElfModule))]
    public class BlockSyncTestAElfModule : AElfModule
    {
        private readonly Dictionary<long,Block> _blockList = new Dictionary<long,Block>();

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<INetworkService>(o =>
            {
                var networkServiceMock = new Mock<INetworkService>();
                networkServiceMock
                    .Setup(p => p.GetBlockByHashAsync(It.IsAny<Hash>(), It.IsAny<string>(), It.IsAny<bool>()))
                    .Returns<Hash, int, bool>((hash, peer, tryOthersIfFail) =>
                    {
                        BlockWithTransactions result = null;
                        if (hash != Hash.Empty)
                        {
                            result = new BlockWithTransactions {Header = _blockList.Last().Value.Header};
                        }

                        return Task.FromResult(result);
                    });

                networkServiceMock
                    .Setup(p => p.GetBlocksAsync(It.IsAny<Hash>(), It.IsAny<long>(), It.IsAny<int>(),
                        It.IsAny<string>(), It.IsAny<bool>()))
                    .Returns<Hash, long, int, string, bool>((previousBlockHash, previousBlockHeight, count, peerPubKey, tryOthersIfFail) =>
                    {
                        var result = new List<BlockWithTransactions>();
                        if (!_blockList.TryGetValue(previousBlockHeight, out var previousBlock) || previousBlock
                                .GetHash() != previousBlockHash)
                        {
                            return Task.FromResult(result);
                        }

                        for (var i = previousBlockHeight + 1; i < previousBlockHeight + count; i++)
                        {
                            if (!_blockList.TryGetValue(i, out var block))
                            {
                                break;
                            }

                            result.Add(new BlockWithTransactions {Header = block.Header});
                        }

                        return Task.FromResult(result);
                    });

                return networkServiceMock.Object;
            });
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var blockchainService = context.ServiceProvider.GetRequiredService<IBlockchainService>();
            var genService = context.ServiceProvider.GetRequiredService<IBlockGenerationService>();
            var exec = context.ServiceProvider.GetRequiredService<IBlockExecutingService>();
            var osTestHelper = context.ServiceProvider.GetService<OSTestHelper>();

            var chain = AsyncHelper.RunSync(() => blockchainService.GetChainAsync());
            var previousBlockHash = chain.BestChainHash;
            var height = chain.BestChainHeight;
            
            foreach (var block in osTestHelper.BestBranchBlockList)
            {
                _blockList.Add(block.Header.Height,block);
            }
            
            var bestBranchHeight = height;

            for (var i = bestBranchHeight; i < bestBranchHeight + 10; i++)
            {
                var newBlock = AsyncHelper.RunSync(() => genService.GenerateBlockBeforeExecutionAsync(new GenerateBlockDto
                {
                    PreviousBlockHash = previousBlockHash,
                    PreviousBlockHeight = height,
                    BlockTime = DateTime.UtcNow
                }));

                // no choice need to execute the block to finalize it.
                var newNewBlock = AsyncHelper.RunSync(() => exec.ExecuteBlockAsync(newBlock.Header, new List<Transaction>(), new List<Transaction>(), CancellationToken.None));

                previousBlockHash = newNewBlock.GetHash();
                height++;
                        
                _blockList.Add(newNewBlock.Header.Height,newNewBlock);
            }
        }
    }
}