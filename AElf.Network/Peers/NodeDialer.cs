using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using AElf.Common.Attributes;
using NLog;

namespace AElf.Network.Peers
{
    public class NodeDialer : INodeDialer
    {
        private ILogger _logger;
        
        public const int DefaultConnectionTimeout = 3000;
        
        public int ReconnectInterval { get; set; } = 3000;
        public int ReconnectTryCount { get; set; } = 3;
        
        private readonly string _ipAddress;
        private readonly int _port;

        public NodeDialer(string ipAddress, int port)
        {
            _ipAddress = ipAddress;
            _port = port;
            
            _logger = LogManager.GetLogger(nameof(NodeDialer));
        }

        public async Task<TcpClient> DialAsync(int timeout = DefaultConnectionTimeout)
        {
            try
            {
                TcpClient tcpClient = new TcpClient();
                Task timeoutTask = Task.Delay(timeout);
                Task connectTask = Task.Run(() => tcpClient.Connect(_ipAddress, _port));
                
                _logger?.Trace($"Dialing {_ipAddress}:{_port}");

                if (await Task.WhenAny(timeoutTask, connectTask) != timeoutTask && tcpClient.Connected)
                    return tcpClient;
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Exception during connection");
            }
            
            return null;
        }

        public async Task<TcpClient> DialWithRetryAsync()
        {
            for (int i = 0; i < ReconnectTryCount; i++)
            {
                _logger.Trace($"Reconnect attempt number {i+1}.");
                
                TcpClient client;
                
                try
                {
                    client = await DialAsync();
                    
                    if (client != null)
                        return client;
                    
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Error dialing the peer.");
                }
                
                await Task.Delay(TimeSpan.FromMilliseconds(ReconnectInterval)); // retry wait
            }

            return null;
        }
    }
}