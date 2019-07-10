using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.OS.Network.Types;
using AElf.Types;

namespace AElf.OS.Network.Infrastructure
{
    public interface IPeerPool
    {
        int PeerCount { get; }
        
        Task<bool> AddPeerAsync(string address);
        Task<bool> RemovePeerByAddressAsync(string address);
        List<IPeer> GetPeers(bool includeFailing = false);
        IPeer GetBestPeer();
        
        IReadOnlyDictionary<long, AcceptedBlockInfo> RecentBlockHeightAndHashMappings { get; }
        IReadOnlyDictionary<long, PreLibBlockInfo> PreLibBlockHeightAndHashMappings { get; }
        
        IPeer FindPeerByAddress(string peerIpAddress);
        IPeer FindPeerByPublicKey(string remotePubKey);

        bool AddPeer(IPeer peer);

        Task<IPeer> RemovePeerAsync(string remotePubKey, bool sendDisconnect);

        Task<Handshake> GetHandshakeAsync();

        void AddRecentBlockHeightAndHash(long blockHeight, Hash blockHash, bool hasFork);

        void AddPreLibBlockHeightAndHash(long blockHeight, Hash blockHash,int preLibCount);
        
        bool HasBlock(long blockHeight, Hash blockHash);

        bool HasPreLib(long blockHeight, Hash blockHash);
        
        Task ClearAllPeersAsync(bool sendDisconnect);
    }
}