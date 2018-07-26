using System;
using AElf.Network.Connection;
using AElf.Network.Data;

namespace AElf.Network.Peers
{
    public interface IPeer
    {
        event EventHandler MessageReceived;
        event EventHandler PeerDisconnected;
        
        string IpAddress { get; }
        ushort Port { get; }
        
        NodeData DistantNodeData { get; }

        bool IsConnected { get; }
        bool IsListening { get; }
        
        void EnqueueOutgoing(Message msg);
    }
}