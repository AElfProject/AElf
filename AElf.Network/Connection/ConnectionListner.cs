using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace AElf.Network.Connection
{
    public class IncomingConnectionArgs : EventArgs
    {
        public TcpClient Client { get; set; }
    }
    
    public class ConnectionListner : IConnectionListener
    {
        // Events have to be handled
        public event EventHandler IncomingConnection;
        public event EventHandler ListeningStopped;  

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
            catch (Exception e)
            {
                // todo log exception in higher level
            }
        }
        
        private async Task AwaitConnection(TcpListener tcpListener)
        {
            TcpClient client = await tcpListener.AcceptTcpClientAsync();
            IncomingConnection?.Invoke(this, new IncomingConnectionArgs { Client = client});
        }
    }
}