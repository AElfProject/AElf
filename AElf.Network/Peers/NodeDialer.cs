using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using AElf.Common.Attributes;

namespace AElf.Network.Peers
{
    [LoggerName("NodeDialer")]
    public class NodeDialer : INodeDialer
    {
        public const int DefaultConnectionTimeout = 3000;
        
        public int ReconnectInterval { get; set; } = 3000;
        public int ReconnectTryCount { get; set; } = 3;
        
        private readonly string _ipAddress;
        private readonly int _port;

        public NodeDialer(string ipAddress, int port)
        {
            _ipAddress = ipAddress;
            _port = port;
        }

        public async Task<TcpClient> DialAsync(int timeout = DefaultConnectionTimeout)
        {
            try
            {
                TcpClient tcpClient = new TcpClient();
                Task timeoutTask = Task.Delay(timeout);
                Task connectTask = Task.Run(() => tcpClient.Connect(_ipAddress, _port));

                if (await Task.WhenAny(timeoutTask, connectTask) != timeoutTask && tcpClient.Connected)
                    return tcpClient;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception during connection");
            }
            
            Console.WriteLine("Could not connect, operation timed out.");
            
            return null;
        }

        public async Task<TcpClient> DialWithRetryAsync()
        {
            for (int i = 0; i < ReconnectInterval; i++)
            {
                Console.WriteLine($"Reconnect attempt number {i+1}.");
                
                TcpClient client;
                
                try
                {
                    client = await DialAsync();
                    
                    if (client != null)
                        return client;
                    
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error dialing the peer.");
                }
                
                await Task.Delay(TimeSpan.FromMilliseconds(ReconnectInterval)); // retry wait
            }

            return null;
        }
    }
}