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

        List<NodeData> GetPeers(ushort? numPeers, bool includeBootnodes = true);

        Task<bool> BroadcastMessage(MessageTypes messageType, byte[] payload, int requestId);
    }
}