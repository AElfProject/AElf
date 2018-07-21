using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AElf.Network.Data;
using AElf.Network.Helpers;
using AElf.Network.Peers.Exceptions;
using Google.Protobuf;
using NLog;

namespace AElf.Network.Peers
{
    public class MessageReceivedArgs : EventArgs
    {
        public Message Message { get; set; }
        public Peer Peer { get; set; }
    }
    
    public class PeerDisconnectedArgs : EventArgs
    {
        public DisconnectReason Reason { get; set; }
        public Peer Peer { get; set; }
    }

    public enum DisconnectReason
    {
        Timeout,
        Auth,
        StreamClosed
    }
    
    public class PeerMessageReceivedArgs : EventArgs
    {
        public Peer Peer { get; set; }
        public Message Message { get; set; }
    }
    
    /// <summary>
    /// This class is essentially a wrapper around the connection. Its the entry
    /// point for incoming messages and is also used for sending messages to the
    /// peer it represents.
    /// </summary>
    public class Peer : IPeer
    {
        private const int DefaultReadTimeOut = 3000;
        private const int BufferSize = 20000;

        private byte[] _receptionBuffer;

        private ILogger _logger;
        
        /// <summary>
        /// The event that's raised when a message is received
        /// from the peer.
        /// </summary>
        public event EventHandler MessageReceived;
        
        /// <summary>
        /// The event that's raised when a peers stream
        /// as ended.
        /// </summary>
        public event EventHandler PeerDisconnected;


        public event EventHandler PeerAuthentified;

        /// <summary>
        /// The data relative to the current nodes identity.
        /// </summary>
        //private NodeData _nodeData; // todo readonly

        private int _port;
        
        private TcpClient _client;
        //private NetworkStream _stream;

        private bool _isListening = false;
        
        public event EventHandler PeerUnreachable;

        private MessageReader _messageReader;
        private MessageWriter _messageWriter;

        /// <summary>
        /// Constructor used for creating a peer that is not
        /// connected to any client. The next logical step
        /// would be to call <see cref="DoConnectAsync"/>.
        /// </summary>
        /// <param name="nodeData"></param>
        /// ///
        /// <param name="localPort"></param>
        /// <param name="peerData"></param>
//        public Peer(int localPort, NodeData peerData)
//        {
//            _port = localPort;
//            
//            DistantNodeData = peerData;
//            _receptionBuffer = new byte[BufferSize];
//
//            _logger = LogManager.GetLogger("Peer");
//        }

        /// <summary>
        /// Constructor used for creating a peer from an
        /// already established connection. The next logical
        /// step is to call <see cref="StartListeningAsync"/>.
        /// </summary>
        /// <param name="nodeData"></param>
        /// <param name="localPort"></param>
        /// <param name="distantNodeData"></param>
        /// <param name="client"></param>
//        public Peer(int localPort, NodeData distantNodeData, TcpClient client)
//        {
//            _port = localPort;
//            DistantNodeData = distantNodeData;
//            
//            _client = client;
//        }

        public Peer(int port)
        {
            _port = port;
            _logger = LogManager.GetLogger("Peer");
        }
        
        public int PacketsReceivedCount { get; private set; }
        public int FailedProtocolCount { get; private set; }
        
        public bool IsAvailable { get; set; }
        public bool IsAuthentified { get; set; }
        
        /// <summary>
        /// The data received after the initial connection.
        /// </summary>
        public NodeData DistantNodeData { get; private set; }

        public bool IsBootnode
        {
            get { return DistantNodeData?.IsBootnode ?? false; }
        }

        public bool IsConnected
        {
            get { return _client != null && _client.Connected; }
        }
        
        public bool IsListening
        {
            get { return IsConnected && _isListening; }
        }

        public string IpAddress
        {
            get { return DistantNodeData?.IpAddress; }
        }

        public ushort Port
        {
            get { return DistantNodeData?.Port != null ? (ushort) DistantNodeData?.Port : (ushort)0; }
        }
        
        /// <summary>
        /// This method set the peers underliying tcp client. This method is intended to be
        /// called when the internal state is clean - meaning that either the object has just
        /// been contructed or has just been closed. 
        /// </summary>
        /// <param name="client"></param>
        /// <param name="reader"></param>
        /// <param name="writer"></param>
        public void Initialize(TcpClient client)
        {
            if (_messageReader != null || _messageWriter != null || _client != null)
            {
                _logger.Trace("Could not initialize, some components aren't cleared.");
            }
            
            try
            {
                _client = client;
            
                var stream = client.GetStream();
            
                MessageReader reader = new MessageReader(stream);
                _messageReader = reader;
            
                MessageWriter writer = new MessageWriter(stream);
                _messageWriter = writer;
                
                _messageReader.PacketReceived += ClientOnPacketReceived;
                _messageReader.StreamClosed += MessageReaderOnStreamClosed;
            
                _messageReader.Start(); 
                _messageWriter.Start();

                SendAuthentification();
                
                IsAvailable = true;
            }
            catch (Exception e)
            {
                _logger.Trace("Error while initializing the connection");
            }
        }

        private void SendAuthentification()
        {
            var nd = new NodeData {Port = _port};
            byte[] packet = nd.ToByteArray();
            
            _messageWriter.EnqueueWork(new Message { Type = (int)MessageType.Auth, Length = packet.Length, Payload = packet});
        }
        
        /// <summary>
        /// This method writes the initial connection information on the peers stream.
        /// Note: for now only the listening port is sent.
        /// </summary>
        /// <returns></returns>
//        public void SendAuthInfo()
//        {
//            try
//            {
//                Message m = new Message();
//                m.Type = (int)MessageTypes.Auth; 
//                    
//                var nd = new NodeData { Port = _port };
//                
//                byte[] packet = nd.ToByteArray();
//                _messageWriter.EnqueueWork(m);
//            }
//            catch (Exception e)
//            {
//                return;
//            }
//        }

        private async void MessageReaderOnStreamClosed(object sender, EventArgs eventArgs)
        {
            Reset();
            
            NodeDialer p = new NodeDialer(IPAddress.Loopback.ToString(), 6789);
            TcpClient client = await p.DialWithRetryAsync();

            if (client != null)
            {
                Initialize(client);
            }
            else
            {
                PeerUnreachable?.Invoke(this, EventArgs.Empty);
            }
        }

        private void Reset()
        {
            if (_messageReader != null)
            {
                _messageReader.PacketReceived -= ClientOnPacketReceived;
                _messageReader.StreamClosed -= MessageReaderOnStreamClosed;
            }
            
            _messageReader?.Close();
            _messageWriter = null;
            
            // todo handle the _message writer
            //_messageWriter.Close();
            _messageWriter = null;
            
            _client?.Close();
            _client = null;
        }

        private void ClientOnPacketReceived(object sender, EventArgs eventArgs)
        {
            try
            {
                if (!(eventArgs is PacketReceivedEventArgs a) || a.Message == null)
                    return;

                if (a.Message.Type == (int)MessageType.Auth)
                {
                    HandleAuthResponse(a.Message);
                }

                if (!IsAuthentified)
                {
                    _logger?.Trace("Received message while not authentified : " + a.Message);
                    return;
                }
                
                PacketsReceivedCount++;
            
                FireMessageReceived(a.Message);
            }
            catch (Exception e)
            {
                _logger.Trace(e);
            }
        }

        // Async response to the connection, when a node connects both sides are 
        // waiting for this to consider the peer usable.
        private void HandleAuthResponse(Message aMessage)
        {
            NodeData n = NodeData.Parser.ParseFrom(aMessage.Payload);
            
            IPEndPoint remoteEndPoint = (IPEndPoint)_client.Client.RemoteEndPoint;
                    
            NodeData distant = new NodeData();
            distant.IpAddress = remoteEndPoint.Address.ToString();
            distant.Port = n.Port;

            DistantNodeData = distant;
            
            IsAuthentified = true;
            
            PeerAuthentified?.Invoke(this, EventArgs.Empty);
            
            _logger?.Trace("Peer authentified");
        }

        private void FireMessageReceived(Message p)
        {
            _logger.Trace("Listeners count " + MessageReceived?.GetInvocationList().Length);
            MessageReceived?.Invoke(this, new PeerMessageReceivedArgs { Peer = this, Message = p });
        }

//        public void SendMessage(Message data)
//        {
//            try
//            {
//                _messageWriter.EnqueueWork(data);
//            }
//            catch (Exception e)
//            {
//                _logger.Trace(e);
//            }
//        }

        public void Disconnect()
        {
            Reset();
        }
        
        /// <summary>
        /// Sends the provided bytes to the peer.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public void EnqueueOutgoing(Message msg)
        {
            if (_messageWriter == null)
            {
                _logger?.Trace($"Peer {DistantNodeData.IpAddress} : {DistantNodeData.Port} - Null stream while sending");
                return;
            }

            try
            {
                _messageWriter.EnqueueWork(msg);
            }
            catch (Exception e)
            {
                _logger.Trace(e, $"Exception while sending data.");
            }
        }
        
        /// <summary>
        /// This method listens for incoming messages from the peer
        /// and raises the corresponding event. This method sets the
        /// <see cref="_isListening"/> field to true.
        /// </summary>
        /// <returns></returns>
//        public async Task StartListeningAsync()
//        {
//            // If the peer is not connected or is already in 
//            // a listening state.
//            if (!IsConnected || IsListening) 
//                return; // todo error
//
//            try
//            {
//                _isListening = true;
//
//                while (true)
//                {
//                    try
//                    {
//                        AElfPacketData packet = await ListenForPacketAsync();
//                        
//                        // raise the event so the higher levels can process it.
//                        var args = new MessageReceivedArgs { Message = packet, Peer = this };
//                        MessageReceived?.Invoke(this, args);
//                    }
//                    catch (InvalidProtocolBufferException invalidProtocol)
//                    {
//                        _logger?.Trace("Received an invalid message", invalidProtocol);
//                    }
//                }
//            }
//            catch (Exception e)
//            {
//                _client?.Close();
//                _isListening = false;
//                
//                var args = new PeerDisconnectedArgs { Peer = this };
//                PeerDisconnected?.Invoke(this, args);
//            }
//            finally
//            {
//                _client?.Close();
//                _isListening = false;
//            }
//        }

//        private async Task<AElfPacketData> ListenForPacketAsync()
//        {
//            byte[] bytes = new byte[20000];
//            int bytesRead = await _stream.ReadAsync(bytes, 0, BufferSize);
//
//            /*byte[] readBytes = new byte[bytesRead];
//            Array.Copy(bytes, readBytes, bytesRead);*/
//            
//            //int bytesRead = await _stream.ReadAsync(_receptionBuffer, 0, BufferSize);
//
//            AElfPacketData packet = null;
//            if (bytesRead > 0)
//            {
//                // Deserialize
//                packet = AElfPacketData.Parser.ParseFrom(bytes, 0, bytesRead);
//                //_logger.Trace("Packet received: " + ((MessageTypes)packet.MsgType) + ", bytes read: " + bytesRead);
//            }
//            else
//            {
//                _client?.Close();
//                _logger?.Trace("Stream closed");
//                throw new Exception("Stream closed");
//            }
//
//            return packet;
//        }



        /// <summary>
        /// This method connects to the peer according
        /// to the information contained in the <see cref="_node"/>
        /// instance.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="OperationCanceledException">Distant peer timeout</exception>
//        public async Task<bool> DoConnectAsync()
//        {
//            if (DistantNodeData == null)
//                return false;
//
//            try
//            {
//                _client = new TcpClient(DistantNodeData.IpAddress, DistantNodeData.Port);
//                _logger?.Trace("Local endpoint:" + ((IPEndPoint)_client?.Client?.LocalEndPoint)?.Address + ":" + ((IPEndPoint)_client?.Client?.LocalEndPoint)?.Port);
//                
//                _stream = _client?.GetStream();
//
//                await WriteConnectInfoAsync();
//                await AwaitForConnectionInfoAsync();
//            }
//            catch (OperationCanceledException e)
//            {
//                _client?.Close();
//                throw new ResponseTimeOutException(e);
//            }
//            catch (Exception e)
//            {
//                return false;
//            }
//
//            return true;
//        }

        /// <summary>
        /// Receives the initial data from the other node
        /// </summary>
        /// <returns></returns>
//        public async Task<NodeData> AwaitForConnectionInfoAsync()
//        {
//            // read the initial data
//            byte[] bytes = new byte[1024]; // todo not every call
//
//            int bytesRead;
//            using (var cancellationTokenSource = new CancellationTokenSource(DefaultReadTimeOut))
//            {
//                Task<int> t = _stream.ReadAsync(bytes, 0, 1024);
//                bytesRead = await t.WithCancellation(cancellationTokenSource.Token);
//            }
//
//            if (bytesRead > 0)
//            {
//                NodeData n = NodeData.Parser.ParseFrom(bytes, 0, bytesRead);
//                return n;
//            }
//
//            return null;
//        }

        public override string ToString()
        {
            return DistantNodeData?.IpAddress + ":" + DistantNodeData?.Port;
        }

        /// <summary>
        /// Equality of two peers is based on the equality of the underlying
        /// distant node data it represents.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, null))
                return false;

            if (ReferenceEquals(obj, this))
                return true;
            
            Peer p = obj as Peer;

            if (p?.DistantNodeData == null || DistantNodeData == null)
                return false;

            return p.DistantNodeData.Equals(DistantNodeData);
        }
    }
}