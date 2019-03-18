using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Modularity;
using AElf.OS.Jobs;
using AElf.OS.Network.Application;
using AElf.OS.Network.Grpc;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

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

                            previousBlockHash = newBlock.GetHash();
                            height++;
                            
                            blockList.Add(newBlock);
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