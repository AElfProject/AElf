using System;
using AElf.Network.Connection;
using AElf.Network.Data;

namespace AElf.Network.Peers
{
    public interface IPeer : IDisposable
    {
        event EventHandler MessageReceived;
        event EventHandler PeerDisconnected;
        event EventHandler AuthFinished;
        
        string IpAddress { get; }
        ushort Port { get; }

        bool IsAuthentified { get; }
        bool IsBp { get; }

        bool Start();
        
        NodeData DistantNodeData { get; }
        byte[] DistantNodeAddress { get; }
        void EnqueueOutgoing(Message msg);
    }
}