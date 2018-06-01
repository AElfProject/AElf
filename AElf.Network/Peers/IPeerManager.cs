using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Network.Data;

namespace AElf.Network.Peers
{
    public interface IPeerManager
    {
        event EventHandler MessageReceived;
        
        void Start();
        void AddPeer(IPeer peer);

        List<NodeData> GetPeers(ushort numPeers);

        Task<bool> BroadcastMessage(MessageTypes messageType, byte[] payload, int requestId);
    }
}