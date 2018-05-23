using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using AElf.Network;

namespace AElf.Kernel.Node.Network.Peers
{
    /// <summary>
    /// The event that's raised when a new connection is
    /// transformed into a Peer.
    /// </summary>
    public class MessageReceivedArgs : EventArgs
    {
        public AElfPacketData Message { get; set; }
        public Peer peer { get; set; }
    }
    
    public class Peer : IPeer
    {
        public event EventHandler MessageReceived;
        
        private readonly TcpClient _client;
        private readonly NetworkStream _stream;
        private readonly NodeData _nodeData;
        
        public Peer(string ipAddress, ushort port)
        {
            _nodeData = new NodeData
            {
                IpAddress = ipAddress,
                Port = port
            };
        }

        public Peer(NodeData nodeData, TcpClient client)
        {
            _nodeData = nodeData;
            _client = client;
            _stream = client?.GetStream();
        }

        public string IpAddress
        {
            get { return _nodeData?.IpAddress; }
        }

        public ushort Port
        {
            get { return _nodeData != null ? (ushort)_nodeData.Port : (ushort)0; }
        }
        
        public async Task StartListeningAsync()
        {
            try
            {
                while (true)
                {
                    // tries to read the 
                    byte[] bytes = new byte[1024];

                    Console.WriteLine("Waiting for a new message.");
                    int bytesRead = await _stream.ReadAsync(bytes, 0, 1024);

                    byte[] readBytes = new byte[bytesRead];
                    Array.Copy(bytes, readBytes, bytesRead);

                    if (bytesRead > 0)
                    {
                        AElfPacketData n = AElfPacketData.Parser.ParseFrom(readBytes);
                       
                        // Raise the event so the higher levels can process it.
                        MessageReceivedArgs args = new MessageReceivedArgs();
                        args.Message = n;
                        args.peer = this;
                        
                        MessageReceived?.Invoke(this, args);
                    }
                    else
                    {
                        Console.WriteLine("End of the stream, closing.");
                        _client?.Close();
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                _client?.Close();
            }
            finally
            {
                _client?.Close();
            }
        }

        public async Task Send(byte[] data)
        {
            if (_stream == null)
                return;

            await _stream.WriteAsync(data, 0, data.Length);
        }
    }
}