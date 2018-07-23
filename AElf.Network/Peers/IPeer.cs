using System;
using System.Threading.Tasks;
using AElf.Network.Connection;
using AElf.Network.Data;
using NodeData = AElf.Network.Data.Protobuf.NodeData;

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