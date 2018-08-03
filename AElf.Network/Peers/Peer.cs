using System;
using System.Net;
using System.Net.Sockets;
using AElf.Network.Connection;
using AElf.Network.Data;
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
        public bool IsClosed { get; private set; }
        
        private const int DefaultReadTimeOut = 3000;
        private const int BufferSize = 20000;

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

        private bool _isListening = false;
        
        public event EventHandler PeerUnreachable;

        private IMessageReader _messageReader;
        private IMessageWriter _messageWriter;
        
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
        public NodeData DistantNodeData { get; set; }

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
                _logger.Trace(e, "Error while initializing the connection");
            }
        }

        /// <summary>
        /// This method writes the initial connection information on the peers stream.
        /// Note: for now only the listening port is sent.
        /// </summary>
        /// <returns></returns>
        private void SendAuthentification()
        {
            var nd = new NodeData {Port = _port};
            byte[] packet = nd.ToByteArray();
            
            _messageWriter.EnqueueMessage(new Message { Type = (int)MessageType.Auth, Length = packet.Length, Payload = packet});
        }

        private async void MessageReaderOnStreamClosed(object sender, EventArgs eventArgs)
        {
            Disconnect();

            if (DistantNodeData == null)
            {
                PeerUnreachable?.Invoke(this, EventArgs.Empty);
            }

            NodeDialer p = new NodeDialer(IPAddress.Loopback.ToString(), DistantNodeData.Port);
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
                _logger?.Trace(e);
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
        }

        private void FireMessageReceived(Message p)
        {
            MessageReceived?.Invoke(this, new PeerMessageReceivedArgs { Peer = this, Message = p });
        }
        
        /// <summary>
        /// Sends the provided bytes to the peer.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public void EnqueueOutgoing(Message msg)
        {
            if (!IsAuthentified)
            {
                _logger?.Trace($"Can't write : not identified {DistantNodeData}.");
            }
            if (_messageWriter == null)
            {
                _logger?.Trace($"Peer {DistantNodeData?.IpAddress} : {DistantNodeData?.Port} - Null stream while sending");
                return;
            }

            try
            {
                _messageWriter.EnqueueMessage(msg);
            }
            catch (Exception e)
            {
                _logger?.Trace(e, $"Exception while sending data.");
            }
        }
        
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

        #region Closing and disposing
        
        public void Disconnect()
        {
            if (_messageReader != null)
            {
                _messageReader.PacketReceived -= ClientOnPacketReceived;
                _messageReader.StreamClosed -= MessageReaderOnStreamClosed;
            }

            Dispose();
        }
        
        public void Dispose()
        {
            _messageReader?.Close();
            _messageWriter?.Close();
            _client?.Close();
        }
        
        #endregion
    }
}