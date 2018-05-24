using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using AElf.Network;
using Google.Protobuf;

namespace AElf.Kernel.Node.Network.Peers
{
    /// <summary>
    /// The event that's raised when a message is received
    /// from the peer.
    /// </summary>
    public class MessageReceivedArgs : EventArgs
    {
        public AElfPacketData Message { get; set; }
        public Peer peer { get; set; }
    }
    
    /// <summary>
    /// This class is essentially a wrapper around the connection. Its the entry
    /// point for incoming messages and is also used for sending messages to the
    /// peer it represents.
    /// </summary>
    public class Peer : IPeer
    {
        public event EventHandler MessageReceived;
        
        private readonly NodeData _nodeData;
        
        private TcpClient _client;
        private NetworkStream _stream;

        private bool _isListening = false;
        
        /// <summary>
        /// Constructor used for creating a peer that is not
        /// connected to any client. The next logical step
        /// would be to call <see cref="DoConnect"/>.
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="port"></param>
        public Peer(string ipAddress, ushort port)
        {
            _nodeData = new NodeData
            {
                IpAddress = ipAddress,
                Port = port
            };
        }

        /// <summary>
        /// Constructor used for creating a peer from an
        /// already established connection. The next logical
        /// step is to call <see cref="StartListeningAsync"/>.
        /// </summary>
        /// <param name="nodeData"></param>
        /// <param name="client"></param>
        public Peer(NodeData nodeData, TcpClient client)
        {
            _nodeData = nodeData;
            _client = client;
            _stream = client?.GetStream();
        }

        public bool IsConnected
        {
            get { return _client != null && _stream != null && _client.Connected; }
        }
        
        public bool IsListening
        {
            get { return IsConnected && _isListening; }
        }

        public string IpAddress
        {
            get { return _nodeData?.IpAddress; }
        }

        public ushort Port
        {
            get { return _nodeData != null ? (ushort)_nodeData.Port : (ushort)0; }
        }
        
        /// <summary>
        /// This method listens for incoming messages from the peer
        /// and raises the corresponding event. This method sets the
        /// <see cref="_isListening"/> field to true.
        /// </summary>
        /// <returns></returns>
        public async Task StartListeningAsync()
        {
            if (!IsConnected || IsListening)
                return; // todo error
            
            try
            {
                while (true)
                {
                    // tries to read the 
                    byte[] bytes = new byte[1024];

                    _isListening = true;
                    int bytesRead = await _stream.ReadAsync(bytes, 0, 1024);

                    byte[] readBytes = new byte[bytesRead];
                    Array.Copy(bytes, readBytes, bytesRead);

                    if (bytesRead > 0)
                    {
                        AElfPacketData n = AElfPacketData.Parser.ParseFrom(readBytes);
                       
                        // raise the event so the higher levels can process it.
                        MessageReceivedArgs args = new MessageReceivedArgs
                        {
                            Message = n,
                            peer = this
                        };

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
                _isListening = false;
            }
            finally
            {
                _client?.Close();
                _isListening = false;
            }
        }

        /// <summary>
        /// Sends the provided bytes to the peer.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task Send(byte[] data)
        {
            if (_stream == null)
                return;

            await _stream.WriteAsync(data, 0, data.Length);
            
            //todo response
        }

        /// <summary>
        /// This method connects to the peer according
        /// to the information contained in the <see cref="_node"/>
        /// instance.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> DoConnect()
        {
            if (_nodeData == null)
                return false;
            
            try
            {
                _client = new TcpClient(_nodeData.IpAddress, _nodeData.Port);
                _stream = _client?.GetStream();
                await WriteConnectInfo();
            }
            catch (Exception e)
            {
                return false;
            }

            return true;
        }
        
        /// <summary>
        /// This method writes the initial connection
        /// information on the peers stream.
        /// </summary>
        /// <returns></returns>
        private async Task<bool> WriteConnectInfo()
        {
            byte[] packet = _nodeData.ToByteArray();
            await _stream.WriteAsync(packet, 0, packet.Length);

            return true;
        }
    }
}