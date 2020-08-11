using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Modularity;
using AElf.OS.Network;
using AElf.OS.Network.Application;
using AElf.OS.Network.Types;
using AElf.Types;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Volo.Abp.Modularity;

namespace AElf.OS.BlockSync
{
    [DependsOn(typeof(BlockSyncTestBaseAElfModule))]
    public class BlockSyncRetryTestAElfModule : AElfModule
    {
        private readonly Dictionary<string, PeerInfo> _peers = new Dictionary<string, PeerInfo>();

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            for (int i = 0; i < 15; i++)
            {
                var pubkey = "PeerPubkey" + i;
                _peers.Add(pubkey, new PeerInfo
                {
                    Pubkey = pubkey,
                    SyncState = SyncState.Finished,
                    LastKnownLibHeight = 150 + i
                });
            }

            var testContext = new BlockSyncRetryTestContext();
            context.Services.AddSingleton<BlockSyncRetryTestContext>(testContext);
            
            context.Services.AddSingleton(o =>
            {
                var networkServiceMock = new Mock<INetworkService>();

                networkServiceMock
                    .Setup(p => p.GetBlocksAsync(It.IsAny<Hash>(), It.IsAny<int>(),
                        It.IsAny<string>()))
                    .Returns<Hash, int, string>((previousBlockHash, count, peerPubKey) =>
                        Task.FromResult(new Response<List<BlockWithTransactions>>()));

                networkServiceMock.Setup(p => p.GetPeers(It.IsAny<bool>())).Returns(_peers.Values.ToList());

                networkServiceMock.Setup(p => p.GetPeerByPubkey(It.IsAny<string>()))
                    .Returns<string>((peerPubKey) => _peers.ContainsKey(peerPubKey) ? _peers[peerPubKey] : null);
                
                testContext.MockedNetworkService = networkServiceMock;
                return networkServiceMock.Object;
            });
        }
    }
    
    public class BlockSyncRetryTestContext
    {
        public Mock<INetworkService> MockedNetworkService { get; set; }
    }
}