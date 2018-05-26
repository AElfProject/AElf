using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Node.Network.Data;
using AElf.Kernel.Node.Network.Helpers;
using AElf.Kernel.Node.Network.Peers.Exceptions;
using Google.Protobuf;

namespace AElf.Kernel.Node.Network.Peers
{
    public class MessageReceivedArgs : EventArgs
    {
        public AElfPacketData Message { get; set; }
        public Peer Peer { get; set; }
    }
    
    /// <summary>
    /// This class is essentially a wrapper around the connection. Its the entry
    /// point for incoming messages and is also used for sending messages to the
    /// peer it represents.
    /// </summary>
    public class Peer : IPeer
    {
        private const int DefaultReadTimeOut = 3000;
        
        /// <summary>
        /// The event that's raised when a message is received
        /// from the peer.
        /// </summary>
        public event EventHandler MessageReceived;
        
        /// <summary>
        /// The data relative to the current nodes identity.
        /// </summary>
        private NodeData _nodeData; // todo readonly
        
        private TcpClient _client;
        private NetworkStream _stream;

        private bool _isListening = false;

        /// <summary>
        /// Constructor used for creating a peer that is not
        /// connected to any client. The next logical step
        /// would be to call <see cref="DoConnect"/>.
        /// </summary>
        /// <param name="nodeData"></param>
        /// <param name="ipAddress"></param>
        /// <param name="port"></param>
        public Peer(NodeData nodeData, string ipAddress, ushort port)
        {
            _nodeData = nodeData;
            DistantNodeData = new NodeData { IpAddress = ipAddress, Port = port };
        }

        /// <summary>
        /// Constructor used for creating a peer from an
        /// already established connection. The next logical
        /// step is to call <see cref="StartListeningAsync"/>.
        /// </summary>
        /// <param name="nodeData"></param>
        /// <param name="distantNodeData"></param>
        /// <param name="client"></param>
        public Peer(NodeData nodeData, NodeData distantNodeData, TcpClient client)
        {
            _nodeData = nodeData;
            DistantNodeData = distantNodeData;
            
            _client = client;
            _stream = client?.GetStream();
        }
        
        /// <summary>
        /// The data received after the initial connection.
        /// </summary>
        public NodeData DistantNodeData { get; private set; }

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

        // todo cf interface comment
        public void SetNodeData(NodeData data)
        {
            _nodeData = data;
        }
        
        /// <summary>
        /// This method listens for incoming messages from the peer
        /// and raises the corresponding event. This method sets the
        /// <see cref="_isListening"/> field to true.
        /// </summary>
        /// <returns></returns>
        public async Task StartListeningAsync()
        {
            // If the peer is not connected or is already in 
            // a listening state.
            if (!IsConnected || IsListening)
                return; // todo error
            
            try
            {
                _isListening = true;
                
                while (true)
                {
                    AElfPacketData packet = await ListenForPacket();
                    
                    // raise the event so the higher levels can process it.
                    var args = new MessageReceivedArgs { Message = packet, Peer = this };
                    MessageReceived?.Invoke(this, args);
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

        private async Task<AElfPacketData> ListenForPacket()
        {
            byte[] bytes = new byte[1024];
            int bytesRead = await _stream.ReadAsync(bytes, 0, 1024);

            byte[] readBytes = new byte[bytesRead];
            Array.Copy(bytes, readBytes, bytesRead);

            AElfPacketData packet = null;
            if (bytesRead > 0)
            {
                // Deserialize
                packet = AElfPacketData.Parser.ParseFrom(readBytes);
            }
            else
            {
                _client?.Close();
                throw new Exception("Stream closed");
            }

            return packet;
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
        /// <exception cref="OperationCanceledException">Distant peer timeout</exception>
        public async Task<bool> DoConnect()
        {
            if (DistantNodeData == null)
                return false;

            try
            {
                _client = new TcpClient(DistantNodeData.IpAddress, DistantNodeData.Port);
                _stream = _client?.GetStream();

                await WriteConnectInfoAsync();
                DistantNodeData = await AwaitForConnectionInfoAsync();
            }
            catch (OperationCanceledException e)
            {
                _client.Close();
                throw new ResponseTimeOutException(e);
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
        public async Task<bool> WriteConnectInfoAsync()
        {
            byte[] packet = _nodeData.ToByteArray();
            await _stream.WriteAsync(packet, 0, packet.Length);

            return true;
        }

        /// <summary>
        /// Receives the initial data from the other node
        /// </summary>
        /// <returns></returns>
        public async Task<NodeData> AwaitForConnectionInfoAsync()
        {
            // read the initial data
            byte[] bytes = new byte[1024]; // todo not every call

            int bytesRead;
            using (var cancellationTokenSource = new CancellationTokenSource(DefaultReadTimeOut))
            {
                Task<int> t = _stream.ReadAsync(bytes, 0, 1024);
                bytesRead = await t.WithCancellation(cancellationTokenSource.Token);
            }

            if (bytesRead > 0)
            {
                NodeData n = NodeData.Parser.ParseFrom(bytes, 0, bytesRead);
                return n;
            }

            return null;
        }

        public override string ToString()
        {
            return DistantNodeData.IpAddress + ":" + DistantNodeData.Port;
        }
    }
}