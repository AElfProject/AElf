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
    public class BlockSyncForkedTestAElfModule : AElfModule
    {
        private readonly List<Block> _blockList = new List<Block>();
        
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
                            result = new BlockWithTransactions {Header = _blockList.Last().Header};
                        }
                        return Task.FromResult(result);
                    });
                
                return networkServiceMock.Object;
            });
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var genService = context.ServiceProvider.GetRequiredService<IBlockGenerationService>();
            var exec = context.ServiceProvider.GetRequiredService<IBlockExecutingService>();
            var osTestHelper = context.ServiceProvider.GetService<OSTestHelper>();
            
            var previousBlockHash = osTestHelper.ForkBranchBlockList.Last().GetHash();
            long height = osTestHelper.ForkBranchBlockList.Last().Height;

            _blockList.Add(osTestHelper.BestBranchBlockList[4]);
            _blockList.AddRange(osTestHelper.ForkBranchBlockList);
            var forkBranchHeight = height;

            for (var i = forkBranchHeight; i < forkBranchHeight + 10; i++)
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
                        
                _blockList.Add(newNewBlock);
            }
            
        }
    }
}