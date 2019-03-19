using System;
using System.Collections.Generic;
using System.Threading;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Modularity;
using AElf.OS.Jobs;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Volo.Abp;
using Volo.Abp.Modularity;
using Xunit.Abstractions;

namespace AElf.OS
{
    [DependsOn(typeof(OSTestAElfModule))]
    public class NetTestAElfModule : AElfModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            context.Services.AddTransient<ForkDownloadJob>();

            context.Services.AddSingleton<INetworkService, NetworkService>();

            context.Services.AddSingleton<IPeerPool>(o =>
            {
                var blockchainService = context.Services.GetRequiredServiceLazy<IBlockchainService>().Value;
                var genService = context.Services.GetRequiredServiceLazy<IBlockGenerationService>().Value;
                var exec = context.Services.GetRequiredServiceLazy<IBlockExecutingService>().Value;

                Mock<IPeer> peerMock = new Mock<IPeer>();

                peerMock.Setup(p => p.GetBlocksAsync(It.IsAny<Hash>(), It.IsAny<int>()))
                    .Returns<Hash, int>(async (hash, cnt) => 
                    {
                        var blockList = new List<Block>();

                        var chain = await blockchainService.GetChainAsync();
                        var previousBlockHash = chain.BestChainHash;
                        long height = chain.BestChainHeight;

                        for (var i = chain.BestChainHeight; i < chain.BestChainHeight + 5; i++)
                        {
                            var newBlock = await genService.GenerateBlockBeforeExecutionAsync(new GenerateBlockDto
                            {
                                PreviousBlockHash = previousBlockHash,
                                PreviousBlockHeight = height,
                                BlockTime = DateTime.UtcNow
                            });

                            // no choice need to execute the block so that Hash doesn't change
                            var newNewBlock = await exec.ExecuteBlockAsync(newBlock.Header, new List<Transaction>(), new List<Transaction>(), CancellationToken.None);

                            previousBlockHash = newNewBlock.GetHash();
                            height++;
                            
                            blockList.Add(newNewBlock);
                        }

                        return blockList;
                    });

                Mock<IPeerPool> peerPoolMock = new Mock<IPeerPool>();
                peerPoolMock.Setup(p => p.FindPeerByAddress(It.IsAny<string>()))
                    .Returns<string>(adr => peerMock.Object);
                peerPoolMock.Setup(p => p.GetPeers(It.IsAny<bool>()))
                    .Returns(new List<IPeer> {peerMock.Object});

                return peerPoolMock.Object;
            });
        }
    }
}