using System;
using System.Threading.Tasks;
using AElf.Kernel.Node.Network.Data;

namespace AElf.Kernel.Node.Network.Peers
{
    public interface IPeer
    {
        event EventHandler MessageReceived;
        
        string IpAddress { get; }
        ushort Port { get; }
        
        NodeData DistantNodeData { get; }

        bool IsConnected { get; }
        bool IsListening { get; }
        
        Task StartListeningAsync();
        Task Send(byte[] data);
        
        Task<bool> DoConnect();
        Task<bool> WriteConnectInfoAsync();
        
        // todo temp - because the peerDatastore return Peers with no "_nodeData" 
        void SetNodeData(NodeData data);
    }
}