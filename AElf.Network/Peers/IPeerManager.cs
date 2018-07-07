using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Network.Data;
using AElf.Network.Peers.Exceptions;

namespace AElf.Network.Peers
{
    public interface IPeerManager
    {
        event EventHandler MessageReceived;
        event EventHandler PeerListEmpty;
        
        event EventHandler PeerAdded;
        event EventHandler PeerRemoved;
        
        void Start();
        bool AddPeer(IPeer peer);
        
        bool NoPeers { get; }

        List<IPeer> GetPeers();
        List<NodeData> GetPeers(ushort? numPeers, bool includeBootnodes = true);

        Task<int> BroadcastMessage(MessageTypes messageType, byte[] payload, int requestId);
    }
}