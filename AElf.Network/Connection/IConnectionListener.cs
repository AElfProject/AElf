using System;
using System.Threading.Tasks;

namespace AElf.Network.Connection
{
    public interface IConnectionListener : IDisposable
    {
        event EventHandler IncomingConnection;
        event EventHandler ListeningStopped; 
        
        Task StartListening(int port);
    }
}