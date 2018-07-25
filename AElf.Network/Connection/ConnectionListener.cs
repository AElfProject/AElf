using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NLog;

namespace AElf.Network.Connection
{
    public class IncomingConnectionArgs : EventArgs
    {
        public TcpClient Client { get; set; }
    }
    
    public class ConnectionListener : IConnectionListener
    {
        private readonly ILogger _logger;
        public event EventHandler IncomingConnection;
        public event EventHandler ListeningStopped;

        public ConnectionListener(ILogger logger)
        {
            _logger = logger;
        }

        public async Task StartListening(int port)
        {
            try
            {
                TcpListener tcpListener = new TcpListener(IPAddress.Any, port);
                tcpListener.Start();
            
                while (true)
                {
                    await AwaitConnection(tcpListener);
                }
            }
            catch (Exception ex)
            {
                _logger.Trace(ex, "Connection listening stopped, no new connections can be made.");
                ListeningStopped?.Invoke(this, EventArgs.Empty);
            }
        }
        
        private async Task AwaitConnection(TcpListener tcpListener)
        {
            TcpClient client = await tcpListener.AcceptTcpClientAsync();
            IncomingConnection?.Invoke(this, new IncomingConnectionArgs { Client = client});
        }
    }
}