using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Node.Domain;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.OS.Network;
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

        public IChainRelatedComponentManager<IAElfNetworkServer> Servers { get; set; }

        protected override async Task ExecuteAsync(ForkDownloadJobArgs args)
        {
            try
            {
                var chainId = args.ChainId;
                var pool = Servers.Get(chainId).PeerPool;
                var chain = await BlockchainService.GetChainAsync(chainId);

                var blockHash = chain.LongestChainHash;
                var blockHeight = chain.LongestChainHeight;
                
                
                var peers = pool.GetPeers().Where(p => p.CurrentBlockHeight > blockHeight);

                //TODO: change to random request to peer, or maybe we can measure the network speed of nodes
                var peer = peers.First();
                
                if(peer.GetBlocksAsync(blockHash,))
                
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"Failed to finish download job");
            }
        }
    }
}