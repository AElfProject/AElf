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
    [DependsOn(typeof(BlockSyncTestBaseAElfModule))]
    public class BlockSyncForkedTestAElfModule : AElfModule
    {
        private readonly Dictionary<Hash,Block> _peerBlockList = new Dictionary<Hash,Block>();
        
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<INetworkService>(o =>
            {
                var networkServiceMock = new Mock<INetworkService>();
                networkServiceMock
                    .Setup(p => p.GetBlocksAsync(It.IsAny<Hash>(), It.IsAny<int>(),
                        It.IsAny<string>()))
                    .Returns<Hash, int, string>((previousBlockHash, count, peerPubKey) =>
                    {
                        var result = new List<BlockWithTransactions>();

                        var hash = previousBlockHash;
                        
                        while (result.Count < count && _peerBlockList.TryGetValue(hash, out var block))
                        {
                            result.Add(new BlockWithTransactions {Header = block.Header});

                            hash = block.GetHash();
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

            foreach (var block in osTestHelper.ForkBranchBlockList)
            {
                _peerBlockList.Add(block.Header.PreviousBlockHash,block);
            }
            
            var forkBranchHeight = height;

            for (var i = forkBranchHeight; i < forkBranchHeight + 20; i++)
            {
                var block = osTestHelper.GenerateBlock(previousBlockHash, height);

                // no choice need to execute the block to finalize it.
                var newBlock = AsyncHelper.RunSync(() => exec.ExecuteBlockAsync(block.Header, new List<Transaction>(), new List<Transaction>(), CancellationToken.None));

                previousBlockHash = newBlock.GetHash();
                height++;
                        
                _peerBlockList.Add(newBlock.Header.PreviousBlockHash, newBlock);
            }
            
        }
    }
}