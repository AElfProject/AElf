using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.OS.Network.Infrastructure
{
    public interface IPeerPool
    {
        int PeerCount { get; }
        
        Task<bool> RemovePeerByAddressAsync(string address);
        List<IPeer> GetPeers(bool includeFailing = false);
        IPeer GetBestPeer(); // todo move to service
        
        IPeer FindPeerByAddress(string peerIpAddress);
        IPeer FindPeerByPublicKey(string remotePubKey);
        
        bool TryAddPeer(IPeer peer);

        Task<IPeer> RemovePeerAsync(string remotePubKey, bool sendDisconnect);
        
        // TODO: consider removing block caching from the pool.
        IReadOnlyDictionary<long, Hash> RecentBlockHeightAndHashMappings { get; }
        void AddRecentBlockHeightAndHash(long blockHeight, Hash blockHash, bool hasFork);

        // TODO move
        Task ClearAllPeersAsync(bool sendDisconnect);
    }
}