using System.Collections.Generic;
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
    public class BlockSyncBadPeerTestAElfModule : AElfModule
    {
        private readonly Dictionary<string, PeerInfo> _peers = new Dictionary<string, PeerInfo>();

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            _peers.Add("BadPeerPubkey", new PeerInfo());

            var osTestHelper = context.Services.GetServiceLazy<OSTestHelper>();
            
            context.Services.AddSingleton(o =>
            {
                var networkServiceMock = new Mock<INetworkService>();
                networkServiceMock
                    .Setup(p => p.GetBlockByHashAsync(It.IsAny<Hash>(), It.IsAny<string>()))
                    .Returns<Hash, string>((hash, peerPubkey) =>
                    {
                        BlockWithTransactions result = null;
                        if (peerPubkey == "BadPeerPubkey")
                        {
                            result = osTestHelper.Value.GenerateBlockWithTransactions(Hash.FromString("BadBlock"),
                                1000);
                        }

                        return Task.FromResult(new Response<BlockWithTransactions>(result));
                    });

//                networkServiceMock
//                    .Setup(p => p.GetBlocksAsync(It.IsAny<Hash>(), It.IsAny<int>(),
//                        It.IsAny<string>()))
//                    .Returns<Hash, int, string>((previousBlockHash, count, peerPubKey) =>
//                    {
//                        var result = new List<BlockWithTransactions>();
//
//                        var hash = previousBlockHash;
//                        
//                        while (result.Count < count && _peerBlockList.TryGetValue(hash, out var block))
//                        {
//                            result.Add(new BlockWithTransactions {Header = block.Header});
//
//                            hash = block.GetHash();
//                        }
//
//                        return Task.FromResult(new Response<List<BlockWithTransactions>>(result));
//                    });

                networkServiceMock.Setup(p => p.RemovePeerByPubkeyAsync(It.IsAny<string>(), It.IsAny<bool>()))
                    .Returns<string, bool>(
                        (peerPubkey, blacklistPeer) =>
                        {
                            _peers.Remove(peerPubkey);
                            return Task.FromResult(true);
                        });

                networkServiceMock.Setup(p => p.GetPeerByPubkey(It.IsAny<string>()))
                    .Returns<string>((peerPubKey) => _peers.ContainsKey(peerPubKey) ? _peers[peerPubKey] : null);

                return networkServiceMock.Object;
            });
        }
    }
}