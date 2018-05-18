using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AElf.Kernel.Node.Network
{
    public class AElfTcpServer : IAElfServer
    {
        //public event EventHandler<ClientAcceptedEventArgs> OnClientAcceptedEvent;

        private readonly List<TcpClient> _connectedClients;
        
        private TcpListener _listener;
        private TcpServerConfig _config;
        
        private CancellationTokenSource _tokenSource;
        private CancellationToken _token;

        public AElfTcpServer() : this(null)
        {
        }

        public AElfTcpServer(TcpServerConfig config)
        {
            if (config == null)
                _config = new TcpServerConfig();
            else
                _config = config;
            
            _connectedClients = new List<TcpClient>();
        }

        public async Task Start(CancellationToken? token = null)
        {
            //---listen at the specified IP and port no.---
            _listener = new TcpListener(IPAddress.Parse(_config.Host), _config.Port);
            try
            {
                _listener.Start();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            
            Console.WriteLine("Listening on " + _listener.LocalEndpoint);
            
            _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(token ?? new CancellationToken());
            _token = _tokenSource.Token;
            
            try
            {
                while (!_token.IsCancellationRequested)
                {
                    Console.WriteLine("Waiting for something to accept...");
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    
                    Task.Run(async () =>
                    {
                        Console.WriteLine("[SERVER] Connection received.");
                        //await Task.Delay(2000);

                        await ProcessIncomingConnection(client);
                        //OnClientAcceptedEvent?.Invoke(this, new ClientAcceptedEventArgs(client.GetStream()));

                        Console.WriteLine("Processing finished...");
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
                Console.WriteLine("[INC CONNECTION] Refused because client was null");
                return;
            }
            
            _connectedClients.Add(client);
            
            // Auth
            string name = await GetNameAsync(client);
            
            Console.WriteLine("[SERVER] Got name : " + name);
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