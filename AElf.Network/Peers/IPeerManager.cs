using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading.Tasks;
using AElf.Network.Data;
using AElf.Network.Peers.Exceptions;
using NodeData = AElf.Network.Data.Protobuf.NodeData;

namespace AElf.Network.Peers
{
    public interface IPeerManager
    {
        event EventHandler MessageReceived;
        event EventHandler PeerListEmpty;
        
        event EventHandler PeerAdded;
        event EventHandler PeerRemoved;
        
        void Start();
        IPeer CreatePeerFromConnection(TcpClient client);
        
        bool NoPeers { get; }

        List<IPeer> GetPeers();
        List<NodeData> GetPeers(ushort? numPeers, bool includeBootnodes = true);

        Task<int> BroadcastMessage(MessageType messageType, byte[] payload);
    }
}