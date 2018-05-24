using System;
using System.Threading.Tasks;

namespace AElf.Kernel.Node.Network.Peers
{
    public interface IPeer
    {
        string IpAddress { get; }
        ushort Port { get; }
        event EventHandler MessageReceived;
        bool IsConnected { get; }
        bool IsListening { get; }
        Task StartListeningAsync();
        Task Send(byte[] data);
        Task<bool> DoConnect();
    }
}