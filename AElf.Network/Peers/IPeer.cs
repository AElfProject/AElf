using System;
using System.Threading.Tasks;
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
        
        Task StartListeningAsync();
        Task SendAsync(byte[] data);
        
        Task<bool> DoConnectAsync();
        Task<bool> WriteConnectInfoAsync();
        
        // todo temp - because the peerDatastore return Peers with no "_nodeData" 
        void SetNodeData(NodeData data);
    }
}