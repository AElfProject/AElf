using System;

namespace AElf.Network.Connection
{
    public interface IMessageReader : IDisposable
    {
        event EventHandler PacketReceived;
        event EventHandler StreamClosed;
        
        void Start();
        void Close();
    }
}