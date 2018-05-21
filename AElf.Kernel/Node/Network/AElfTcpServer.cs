using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Node.Network.Helpers;
using NLog;

namespace AElf.Kernel.Node.Network
{
    public class AElfTcpServer : IAElfServer
    {
        private readonly List<TcpClient> _connectedClients;
        
        private TcpListener _listener;
        private TcpServerConfig _config;

        private ILogger _logger;
        
        private CancellationTokenSource _tokenSource;
        private CancellationToken _token;

        public AElfTcpServer(ILogger logger) : this(null, logger)
        {
        }

        public AElfTcpServer(TcpServerConfig config, ILogger logger)
        {
            if (config == null)
                _config = new TcpServerConfig();
            else
                _config = config;
            
            _connectedClients = new List<TcpClient>();
            _logger = logger;
        }

        public async Task Start(CancellationToken? token = null)
        {
            try
            {
                _listener = new TcpListener(IPAddress.Parse(_config.Host), _config.Port);
                _listener.Start();
            }
            catch (Exception e)
            {
                _logger.Error(e, "An error occurred while starting the server");
            }
            
            _logger.Info("Server listening on " + _listener.LocalEndpoint);
            
            _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token ?? new CancellationToken());
            _token = _tokenSource.Token;
            
            try
            {
                while (!_token.IsCancellationRequested)
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    
                    Task.Run(async () =>
                    {
                        _logger.Info("Connection received.");

                        await ProcessIncomingConnection(client);

                    }, _token);
                }
            }
            finally
            {
                _listener.Stop();
            }
        }
        
        private async Task ProcessIncomingConnection(TcpClient client)
        {
            if (client == null)
            {
                _logger.Error("Connection refused: null client");
                return;
            }
            
            _connectedClients.Add(client);
            
            // Fake auth - todo
            string name = await GetNameAsync(client);
            
            _logger.Info("Connection established");
        }

        private async Task<string> GetNameAsync(TcpClient client)
        {
            byte[] askName = Encoding.UTF8.GetBytes("GetName");

            NetworkStream stream = client.GetStream();
            await stream.WriteAsync(askName, 0, askName.Length);
            
            byte[] buffer = new byte[7];
            await stream.ReadAsync(buffer, 0, 7);
            
            return buffer.ToUtf8();
        }
    }
}