using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Timers;
using AElf.Network.Connection;
using AElf.Network.Data;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using NLog;

namespace AElf.Network.Peers
{
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
        
        public bool IsConsensus
        {
            get { return Message?.IsConsensus ?? false; }
        } 
    }
    
    /// <summary>
    /// This class is essentially a wrapper around the connection. Its the entry
    /// point for incoming messages and is also used for sending messages to the
    /// peer it represents.
    /// </summary>
    public class Peer : IPeer
    {
        private const string LoggerName = "Peer";
        
        private readonly ILogger _logger;
        private readonly IMessageReader _messageReader;
        private readonly IMessageWriter _messageWriter;
        
        private readonly Timer _pingPongTimer;
        
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

        /// <summary>
        /// The event that's raised when the authentification phase has finished.
        /// </summary>
        public event EventHandler AuthFinished; 
        
        public bool IsClosed { get; private set; }
        public bool IsAuthentified { get; private set; }

        /// <summary>
        /// This nodes listening port.
        /// </summary>
        private int _port;
        
        private TcpClient _client;
        
        public int PacketsReceivedCount { get; private set; }
        
        private readonly double _defaultPingInterval = TimeSpan.FromSeconds(1).TotalMilliseconds;
        
        private readonly List<Ping> _pings = new List<Ping>();
        private object pingLock = new Object();
        
        private TimeSpan pingWaitTime = TimeSpan.FromSeconds(2);

        private int _droppedPings = 0;
        
        /// <summary>
        /// The data received after the initial connection.
        /// </summary>
        public NodeData DistantNodeData { get; set; }

        public bool IsConnected
        {
            get { return _client != null && _client.Connected; }
        }

        public string IpAddress
        {
            get { return DistantNodeData?.IpAddress; }
        }

        public ushort Port
        {
            get { return DistantNodeData?.Port != null ? (ushort) DistantNodeData?.Port : (ushort)0; }
        }
        
        public Peer(TcpClient client, IMessageReader reader, IMessageWriter writer, int port)
        {
            _pingPongTimer = new Timer();
            SetupHeartbeat();
            
            _port = port;
            _logger = LogManager.GetLogger(LoggerName);
            
            _client = client;
            
            _messageReader = reader;
            _messageWriter = writer;
        }

        private void SetupHeartbeat()
        {
            _pingPongTimer.Interval = _defaultPingInterval;
            _pingPongTimer.Elapsed += TimerTimeoutElapsed;
            _pingPongTimer.AutoReset = true;
        }
        
        public bool Start()
        {
            if (_messageReader == null || _messageWriter == null || _client == null)
            {
                _logger.Trace("Could not initialize, null components.");
                return false;
            }
            
            try
            {
                _messageReader.PacketReceived += ClientOnPacketReceived;
                _messageReader.StreamClosed += MessageReaderOnStreamClosed;
            
                _messageReader.Start();
                _messageWriter.Start();

                SendAuthentification();
            }
            catch (Exception e)
            {
                _logger.Trace(e, "Error while initializing the connection");
                return false;
            }

            return true;
        }

        private void MessageReaderOnStreamClosed(object sender, EventArgs eventArgs)
        {
            Disconnect();
        }

        private void ClientOnPacketReceived(object sender, EventArgs eventArgs)
        {
            try
            {
                if (!(eventArgs is PacketReceivedEventArgs a) || a.Message == null)
                    return;

                if (a.Message.Type == (int) MessageType.Auth)
                {
                    HandleAuthResponse(a.Message);
                    return;
                }
                
                if (!IsAuthentified)
                {
                    _logger?.Trace("Received message while not authentified : " + a.Message);
                    return;
                }

                if (a.Message.Type == (int) MessageType.Ping)
                {
                    HandlePingMessage(a.Message);
                    return;
                }

                if (a.Message.Type == (int) MessageType.Pong)
                {
                    HandlePongMessage(a.Message);
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

        #region Heartbeat
        
        private void TimerTimeoutElapsed(object sender, ElapsedEventArgs e)
        {
            if (IsClosed)
                return;

            lock (pingLock)
            {
                DateTime lowerThreshold = DateTime.Now - pingWaitTime;
                var pings = _pings.Where(p => p.Time.ToDateTime() < lowerThreshold).ToList();

                if (pings.Count > 0)
                {
                    _droppedPings += pings.Count;
                    _logger?.Trace($"{DistantNodeData} - Current failed count {_droppedPings}.");
                    
                    var peerStr = _pings.Select(c => c.Id).Aggregate((a, b) => a.ToString() + ", " + b);
                    
                    _logger?.Trace($"{DistantNodeData} - {pings.Count} pings where dropped [ {peerStr} ].");
                    
                    foreach (var p in pings)
                        _pings.Remove(p);
                }
            }
            
            // Create a new ping
            try
            {
                Guid id = Guid.NewGuid();
                Ping ping = new Ping { Id = id.ToString(), Time = Timestamp.FromDateTime(DateTime.UtcNow)};
            
                byte[] payload = ping.ToByteArray();
            
                var pingMsg = new Message { Type = (int)MessageType.Ping, Length = payload.Length, Payload = payload };

                lock (pingLock)
                {
                    _pings.Add(ping);
                }

                Task.Run(() => EnqueueOutgoing(pingMsg));
            }
            catch (Exception exception)
            {
                _logger?.Trace(exception, "Error while sending ping message.");
            }
        }

        private void HandlePingMessage(Message pingMsg)
        {
            try
            {
                Ping ping = Ping.Parser.ParseFrom(pingMsg.Payload);
                Pong pong = new Pong { Id = ping.Id, Time = Timestamp.FromDateTime(DateTime.UtcNow) };
                        
                byte[] payload = pong.ToByteArray();
            
                var pongMsg = new Message
                {
                    Type = (int)MessageType.Pong,
                    Length = payload.Length,
                    Payload = payload
                };

                EnqueueOutgoing(pongMsg);
            }
            catch (Exception e)
            {
                _logger?.Trace(e, $"Failed to process ping message from {DistantNodeData}.");
            }
        }

        private void HandlePongMessage(Message pongMsg)
        {
            try
            {
                Pong pong = Pong.Parser.ParseFrom(pongMsg.Payload);
                
                lock (pingLock)
                {
                    Ping ping = _pings.FirstOrDefault(p => p.Id == pong.Id);
                            
                    if (ping != null)
                        _pings.Remove(ping);
                    else
                        _logger?.Trace($"Could not match pong reply {pong.Id}.");
                }
            }
            catch (Exception e)
            {
                _logger?.Trace(e, $"Failed to handle pong message from {DistantNodeData}.");
            }
        }

        #endregion Heartbeat

        #region Authentification
        
        /// <summary>
        /// This method writes the initial connection information on the peers stream.
        /// Note: for now only the listening port is sent.
        /// </summary>
        /// <returns></returns>
        private void SendAuthentification()
        {
            var nd = new NodeData { Port = _port };
            byte[] packet = nd.ToByteArray();
            
            _messageWriter.EnqueueMessage(new Message { Type = (int)MessageType.Auth, Length = packet.Length, Payload = packet});
        }
       
        private void HandleAuthResponse(Message aMessage)
        {
            NodeData n = NodeData.Parser.ParseFrom(aMessage.Payload);
            
            IPEndPoint remoteEndPoint = (IPEndPoint)_client.Client.RemoteEndPoint;
                    
            NodeData distant = new NodeData();
            distant.IpAddress = remoteEndPoint.Address.ToString();
            distant.Port = n.Port;

            DistantNodeData = distant;
            
            IsAuthentified = true;
            
            _pingPongTimer.Start();
            
            AuthFinished?.Invoke(this, EventArgs.Empty);
        }

        #endregion Authentification

        private void FireMessageReceived(Message p)
        {
            MessageReceived?.Invoke(this, new PeerMessageReceivedArgs { Peer = this, Message = p });
        }
        
        /// <summary>
        /// Sends the provided message to the peer.
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
            Dispose();
        }
        
        public void Dispose()
        {
            if (_messageReader != null)
            {
                _messageReader.PacketReceived -= ClientOnPacketReceived;
                _messageReader.StreamClosed -= MessageReaderOnStreamClosed;
            }
            
            _messageReader?.Close();
            _messageWriter?.Close();
            _client?.Close();
            
            _pingPongTimer.Stop();
        }
        
        #endregion
    }
}