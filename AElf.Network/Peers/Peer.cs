﻿using System;
using System.Net;
using System.Net.Sockets;
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
        public AElfPacketData Message { get; set; }
        public Peer Peer { get; set; }
    }
    
    public class PeerDisconnectedArgs : EventArgs
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

        /// <summary>
        /// The data relative to the current nodes identity.
        /// </summary>
        //private NodeData _nodeData; // todo readonly

        private int _port;
        
        private TcpClient _client;
        private NetworkStream _stream;

        private bool _isListening = false;

        /// <summary>
        /// Constructor used for creating a peer that is not
        /// connected to any client. The next logical step
        /// would be to call <see cref="DoConnectAsync"/>.
        /// </summary>
        /// <param name="nodeData"></param>
        /// ///
        /// <param name="localPort"></param>
        /// <param name="peerData"></param>
        public Peer(int localPort, NodeData peerData)
        {
            _port = localPort;
            
            DistantNodeData = peerData;
            _receptionBuffer = new byte[BufferSize];

            _logger = LogManager.GetLogger("Peer");
        }

        /// <summary>
        /// Constructor used for creating a peer from an
        /// already established connection. The next logical
        /// step is to call <see cref="StartListeningAsync"/>.
        /// </summary>
        /// <param name="nodeData"></param>
        /// <param name="localPort"></param>
        /// <param name="distantNodeData"></param>
        /// <param name="client"></param>
        public Peer(int localPort, NodeData distantNodeData, TcpClient client)
        {
            _port = localPort;
            DistantNodeData = distantNodeData;
            
            _client = client;
            _stream = client?.GetStream();
        }
        
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
            get { return _client != null && _stream != null && _client.Connected; }
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
                    try
                    {
                        AElfPacketData packet = await ListenForPacketAsync();
                        
                        // raise the event so the higher levels can process it.
                        var args = new MessageReceivedArgs { Message = packet, Peer = this };
                        MessageReceived?.Invoke(this, args);
                    }
                    catch (InvalidProtocolBufferException invalidProtocol)
                    {
                        _logger?.Trace("Received an invalid message", invalidProtocol);
                    }
                }
            }
            catch (Exception e)
            {
                _client?.Close();
                _isListening = false;
                
                var args = new PeerDisconnectedArgs { Peer = this };
                PeerDisconnected?.Invoke(this, args);
            }
            finally
            {
                _client?.Close();
                _isListening = false;
            }
        }

        private async Task<AElfPacketData> ListenForPacketAsync()
        {
            byte[] bytes = new byte[20000];
            int bytesRead = await _stream.ReadAsync(bytes, 0, BufferSize);

            /*byte[] readBytes = new byte[bytesRead];
            Array.Copy(bytes, readBytes, bytesRead);*/
            
            //int bytesRead = await _stream.ReadAsync(_receptionBuffer, 0, BufferSize);

            AElfPacketData packet = null;
            if (bytesRead > 0)
            {
                // Deserialize
                packet = AElfPacketData.Parser.ParseFrom(bytes, 0, bytesRead);
                //Console.WriteLine("Packet received: " + ((MessageTypes)packet.MsgType) + ", bytes read: " + bytesRead);
            }
            else
            {
                _client?.Close();
                _logger?.Trace("Stream closed");
                throw new Exception("Stream closed");
            }

            return packet;
        }

        /// <summary>
        /// Sends the provided bytes to the peer.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task SendAsync(byte[] data)
        {
            if (_stream == null)
            {
                _logger?.Trace($"Peer {DistantNodeData.IpAddress} : {DistantNodeData.Port} - Null stream while sending");
                return;
            }

            try
            {
                await _stream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception e)
            {
                _logger.Trace(e, $"Exception while sending data.");
            }
        }

        /// <summary>
        /// This method connects to the peer according
        /// to the information contained in the <see cref="_node"/>
        /// instance.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="OperationCanceledException">Distant peer timeout</exception>
        public async Task<bool> DoConnectAsync()
        {
            if (DistantNodeData == null)
                return false;

            try
            {
                _client = new TcpClient(DistantNodeData.IpAddress, DistantNodeData.Port);
                _logger?.Trace("Local endpoint:" + ((IPEndPoint)_client?.Client?.LocalEndPoint)?.Address + ":" + ((IPEndPoint)_client?.Client?.LocalEndPoint)?.Port);
                
                _stream = _client?.GetStream();

                await WriteConnectInfoAsync();
                await AwaitForConnectionInfoAsync();
            }
            catch (OperationCanceledException e)
            {
                _client?.Close();
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
            try
            {
                var nd = new NodeData {Port = _port};
                
                byte[] packet = nd.ToByteArray();
                await _stream.WriteAsync(packet, 0, packet.Length);
            }
            catch (Exception e)
            {
                return false;
            }

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