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
        
        bool IsBootnode { get; }
        
        Task StartListeningAsync();
        Task SendAsync(byte[] data);
        
        Task<bool> DoConnectAsync();
        Task<bool> WriteConnectInfoAsync();
    }
}