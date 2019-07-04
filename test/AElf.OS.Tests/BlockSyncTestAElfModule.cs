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
    public class BlockSyncTestAElfModule : AElfModule
    {
        private readonly Dictionary<Hash,Block> _peerBlockList = new Dictionary<Hash,Block>();

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<INetworkService>(o =>
            {
                var networkServiceMock = new Mock<INetworkService>();
                networkServiceMock
                    .Setup(p => p.GetBlockByHashAsync(It.IsAny<Hash>(), It.IsAny<string>()))
                    .Returns<Hash, int>((hash, peer) =>
                    {
                        BlockWithTransactions result = null;
                        if (hash != Hash.Empty)
                        {
                            var blockchainService = context.Services.GetServiceLazy<IBlockchainService>().Value;
                            var chain = AsyncHelper.RunSync(() => blockchainService.GetChainAsync());
                            result = new BlockWithTransactions {Header = _peerBlockList[chain.BestChainHash].Header};
                        }

                        return Task.FromResult(result);
                    });

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
            var blockchainService = context.ServiceProvider.GetRequiredService<IBlockchainService>();
            var genService = context.ServiceProvider.GetRequiredService<IBlockGenerationService>();
            var exec = context.ServiceProvider.GetRequiredService<IBlockExecutingService>();
            var osTestHelper = context.ServiceProvider.GetService<OSTestHelper>();

            var chain = AsyncHelper.RunSync(() => blockchainService.GetChainAsync());
            var previousBlockHash = chain.BestChainHash;
            var height = chain.BestChainHeight;
            
            foreach (var block in osTestHelper.BestBranchBlockList)
            {
                _peerBlockList.Add(block.Header.PreviousBlockHash,block);
            }
            
            var bestBranchHeight = height;

            for (var i = bestBranchHeight; i < bestBranchHeight + 20; i++)
            {
                var block = osTestHelper.GenerateBlock(previousBlockHash, height);

                // no choice need to execute the block to finalize it.
                var newBlock = AsyncHelper.RunSync(() => exec.ExecuteBlockAsync(block.Header, new List<Transaction>(), new List<Transaction>(), CancellationToken.None));

                previousBlockHash = newBlock.GetHash();
                height++;
                        
                _peerBlockList.Add(newBlock.Header.PreviousBlockHash,newBlock);
            }
        }
    }
}