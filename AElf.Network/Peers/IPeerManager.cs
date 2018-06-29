using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Network.Data;

namespace AElf.Network.Peers
{
    public interface IPeerManager
    {
        event EventHandler MessageReceived;
        event EventHandler PeerListEmpty;
        
        void Start();
        bool AddPeer(IPeer peer);
        
        bool NoPeers { get; }

        List<IPeer> GetPeers();
        List<NodeData> GetPeers(ushort? numPeers, bool includeBootnodes = true);

        Task<int> BroadcastMessage(MessageTypes messageType, byte[] payload, int requestId);
    }
}