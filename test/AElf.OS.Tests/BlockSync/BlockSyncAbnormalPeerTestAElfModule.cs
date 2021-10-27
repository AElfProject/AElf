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
    public class BlockSyncAbnormalPeerTestAElfModule : AElfModule
    {
        private readonly Dictionary<string, PeerInfo> _peers = new Dictionary<string, PeerInfo>();

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            _peers.Add("AbnormalPeerPubkey", new PeerInfo());
            _peers.Add("NotLinkedBlockPubkey", new PeerInfo());
            _peers.Add("WrongLIBPubkey", new PeerInfo
            {
                Pubkey = "WrongLIBPubkey",
                SyncState = SyncState.Finished,
                LastKnownLibHeight = 110
            });

            for (int i = 0; i < 15; i++)
            {
                var pubkey = "GoodPeerPubkey" + i;
                _peers.Add(pubkey, new PeerInfo
                {
                    Pubkey = pubkey,
                    SyncState = SyncState.Finished,
                    LastKnownLibHeight = 150 + i
                });
            }

            var osTestHelper = context.Services.GetServiceLazy<OSTestHelper>();
            
            context.Services.AddSingleton(o =>
            {
                var networkServiceMock = new Mock<INetworkService>();
                networkServiceMock
                    .Setup(p => p.GetBlockByHashAsync(It.IsAny<Hash>(), It.IsAny<string>()))
                    .Returns<Hash, string>((hash, peerPubkey) =>
                    {
                        BlockWithTransactions result = null;
                        if (peerPubkey == "AbnormalPeerPubkey")
                        {
                            result = osTestHelper.Value.GenerateBlockWithTransactions(HashHelper.ComputeFrom("BadBlock"),
                                1000);
                        }

                        return Task.FromResult(new Response<BlockWithTransactions>(result));
                    });

                networkServiceMock
                    .Setup(p => p.GetBlocksAsync(It.IsAny<Hash>(), It.IsAny<int>(),
                        It.IsAny<string>()))
                    .Returns<Hash, int, string>((previousBlockHash, count, peerPubKey) =>
                    {
                        var result = new List<BlockWithTransactions>();
                        var hash = previousBlockHash;
                        
                        if (peerPubKey == "NotLinkedBlockPubkey")
                        {
                            for (var i = 0; i < count-1; i++)
                            {
                                var block = osTestHelper.Value.GenerateBlockWithTransactions(hash, 100 + i);
                                hash = block.Header.PreviousBlockHash;

                                result.Add(block);
                            }

                            var notLinkedBlock =
                                osTestHelper.Value.GenerateBlockWithTransactions(HashHelper.ComputeFrom("NotLinkedBlock"),
                                    100);
                            result.Add(notLinkedBlock);
                        }

                        if (hash == HashHelper.ComputeFrom("GoodBlockHash"))
                        {
                            for (var i = 0; i < count; i++)
                            {
                                var block = osTestHelper.Value.GenerateBlockWithTransactions(hash, 100 + i);
                                hash = block.Header.PreviousBlockHash;

                                result.Add(block);
                            }
                        }

                        return Task.FromResult(new Response<List<BlockWithTransactions>>(result));
                    });

                networkServiceMock.Setup(p => p.RemovePeerByPubkeyAsync(It.IsAny<string>(), It.IsAny<int>()))
                    .Returns<string, int>(
                        (peerPubkey,removalSeconds) =>
                        {
                            _peers.Remove(peerPubkey);
                            return Task.FromResult(true);
                        });

                networkServiceMock.Setup(p => p.GetPeerByPubkey(It.IsAny<string>()))
                    .Returns<string>((peerPubKey) => _peers.ContainsKey(peerPubKey) ? _peers[peerPubKey] : null);

                networkServiceMock.Setup(p => p.GetPeers(It.IsAny<bool>())).Returns(_peers.Values.ToList());

                return networkServiceMock.Object;
            });
        }
    }
}