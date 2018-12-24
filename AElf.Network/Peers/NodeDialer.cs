using System;
using System.Net.Sockets;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Network.Peers
{
    public class NodeDialer : INodeDialer
    {
        public ILogger<NodeDialer> Logger {get;set;}
        
        public const int DefaultConnectionTimeout = 3000;
        
        public int ReconnectInterval { get; set; } = 3000;
        public int ReconnectTryCount { get; set; } = 3;
        
        private readonly string _ipAddress;
        private readonly int _port;

        public NodeDialer(string ipAddress, int port)
        {
            _ipAddress = ipAddress;
            _port = port;

            Logger = NullLogger<NodeDialer>.Instance;
        }

        public async Task<TcpClient> DialAsync(int timeout = DefaultConnectionTimeout)
        {
            try
            {
                TcpClient tcpClient = new TcpClient();
                Task timeoutTask = Task.Delay(timeout);
                Task connectTask = Task.Run(() => tcpClient.Connect(_ipAddress, _port));
                
                Logger.LogTrace($"Dialing {_ipAddress}:{_port}.");

                if (await Task.WhenAny(timeoutTask, connectTask) != timeoutTask && tcpClient.Connected)
                    return tcpClient;
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Exception during connection.");
            }
            
            return null;
        }

        public async Task<TcpClient> DialWithRetryAsync()
        {
            for (int i = 0; i < ReconnectTryCount; i++)
            {
                Logger.LogTrace($"Reconnect attempt number {i+1}.");
                
                TcpClient client;
                
                try
                {
                    client = await DialAsync();
                    
                    if (client != null)
                        return client;
                    
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Error dialing the peer.");
                }
                
                await Task.Delay(TimeSpan.FromMilliseconds(ReconnectInterval)); // retry wait
            }

            return null;
        }
    }
}