using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Types;

namespace AElf.OS.Network.Infrastructure
{
    public interface IPeerPool
    {
        int PeerCount { get; }
        
        // TODO: remove this method from here, it contains dialing logic and peer creation.
        Task<bool> AddPeerAsync(string address);
        
        Task<bool> RemovePeerByAddressAsync(string address);
        List<IPeer> GetPeers(bool includeFailing = false);
        IPeer GetBestPeer();
        
        IPeer FindPeerByAddress(string peerIpAddress);
        IPeer FindPeerByPublicKey(string remotePubKey);
        
        bool AddPeer(IPeer peer);

        Task<IPeer> RemovePeerAsync(string remotePubKey, bool sendDisconnect);
        
        // TODO: consider removing block caching from the pool.
        IReadOnlyDictionary<long, Hash> RecentBlockHeightAndHashMappings { get; }
        void AddRecentBlockHeightAndHash(long blockHeight, Hash blockHash, bool hasFork);
        
        // TODO: remove handshake logic from the pool.
        Task<Handshake> GetHandshakeAsync();
        
        Task ClearAllPeersAsync(bool sendDisconnect);
    }
}