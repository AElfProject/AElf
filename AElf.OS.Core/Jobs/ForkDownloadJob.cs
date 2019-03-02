using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Node.Domain;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using AElf.OS.Network.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace AElf.OS.Jobs
{
    public class ForkDownloadJob : AsyncBackgroundJob<ForkDownloadJobArgs>
    {
        public IBlockchainService BlockchainService { get; set; }

        public IBlockchainExecutingService BlockchainExecutingService { get; set; }
        public INetworkService NetworkService { get; set; }

        public IAElfNetworkServer Server { get; set; }

        protected override async Task ExecuteAsync(ForkDownloadJobArgs args)
        {
            try
            {
                var count = 5;

                var chainId = args.ChainId;
                var pool = Server.PeerPool;


                while (true)
                {
                    var chain = await BlockchainService.GetChainAsync();

                    var blockHash = chain.LongestChainHash;
                    var blockHeight = chain.LongestChainHeight;

                    var peers = pool.GetPeers().Where(p => p.CurrentBlockHeight > blockHeight);

                    //TODO: change to random request to peer, or maybe we can measure the network speed of nodes
                    var peer = peers.First();

                    var blocks = await peer.GetBlocksAsync(blockHash, count);

                    foreach (var block in blocks)
                    {
                        var status = await BlockchainService.AttachBlockToChainAsync(chain, block);
                        await BlockchainExecutingService.ExecuteBlocksAttachedToLongestChain(chain, status);
                    }

                    if (chain.LongestChainHeight > args.BlockHeight || blocks.Count == 0)
                        break;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Failed to finish download job");
            }
        }
    }
}