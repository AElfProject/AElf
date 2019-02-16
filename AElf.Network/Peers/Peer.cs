using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Timers;
using AElf.Common;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Account;
using AElf.Network.Connection;
using AElf.Network.Data;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;

namespace AElf.Network.Peers
{
    public class PeerDisconnectedArgs : EventArgs
    {
        public DisconnectReason Reason { get; set; }
        public IPeer Peer { get; set; }
    }

    public class AuthFinishedArgs : EventArgs
    {
        public bool IsAuthentified { get; private set; }
        public RejectReason Reason { get; private set; }

        public AuthFinishedArgs(RejectReason reason)
        {
            IsAuthentified = false;
            Reason = reason;
        }

        public AuthFinishedArgs()
        {
            IsAuthentified = true;
        }
    }

    public enum RejectReason
    {
        AuthTimeout,
        AuthWrongVersion,
        AuthInvalidHandshakeMsg,
        AuthInvalidKey,
        AuthInvalidSig,
        None
    }

    public enum DisconnectReason
    {
        StreamClosed,
        BlockRequestTimeout
    }

    public class PeerMessageReceivedArgs : EventArgs
    {
        public Peer Peer { get; set; }
        public Message Message { get; set; }

        public Block Block { get; set; }
    }

    /// <summary>
    /// This class is essentially a wrapper around the connections underlying stream. Its the entry
    /// point for incoming messages and is also used for sending messages to the peer it represents.
    /// This class handles a basic form of authentification as well as ping messages.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public partial class Peer : IPeer
    {
        private const string LoggerName = "Peer";

        private const double DefaultPingInterval = 1000;
        private const double DefaultAuthTimeout = 2000;

        public ILogger<Peer> Logger { get; set; }
        private readonly IMessageReader _messageReader;
        private readonly IMessageWriter _messageWriter;
        private readonly IAccountService _accountService;

        private readonly Timer _authTimer;

        /// <summary>
        /// The event that's raised when a message is received from the peer.
        /// </summary>
        public event EventHandler MessageReceived;

        /// <summary>
        /// The event that's raised when a peers stream has ended.
        /// </summary>
        public event EventHandler PeerDisconnected;

        /// <summary>
        /// The event that's raised when the authentification phase has finished.
        /// </summary>
        public event EventHandler AuthFinished;
        
        /// <summary>
        /// the event that's raised when a request fails.
        /// </summary>
        public event EventHandler RequestFailed;

        /// <summary>
        /// Indicates if Dispose has been called (once false, never changes back to true).
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// Indicates if correct authentication information has been received.
        /// </summary>
        public bool IsAuthentified { get; private set; }

        /// <summary>
        /// This nodes listening port.
        /// </summary>
        private readonly int _port;

        /// <summary>
        /// The underlying network client.
        /// </summary>
        private readonly TcpClient _client;

        public int PacketsReceivedCount { get; private set; }

        public double AuthTimeout { get; set; } = DefaultAuthTimeout;

        /// <summary>
        /// The data received in the handshake message.
        /// </summary>
        [JsonProperty(PropertyName = "Address")]
        public NodeData DistantNodeData => _lastReceivedHandshake?.NodeInfo ?? new NodeData
        {
            IpAddress = IsDisposed ? "disposed" : ((IPEndPoint)_client.Client.RemoteEndPoint).Address.ToString(),
            Port = IsDisposed ? 0 : ((IPEndPoint)_client.Client.RemoteEndPoint).Port
        };

        private Handshake _lastReceivedHandshake;

        public byte[] DistantPubKey { get; private set; }

        public string DistantNodeAddress { get; private set; }
        public byte[] DistantPublicKey => _lastReceivedHandshake?.PublicKey.ToByteArray();

        [JsonProperty(PropertyName = "IsBp")] public bool IsBp { get; internal set; }

        public string IpAddress => DistantNodeData?.IpAddress;

        public ushort Port => DistantNodeData?.Port != null ? (ushort) DistantNodeData?.Port : (ushort) 0;

        public readonly int CurrentHeight;

        public Peer(TcpClient client, IMessageReader reader, IMessageWriter writer, int port, 
            int currentHeight, IAccountService accountService)
        {
            BlockRequests = new List<TimedBlockRequest>();
            _announcements = new List<Announce>();

            _pingPongTimer = new Timer();
            _authTimer = new Timer();

            SetupHeartbeat();

            _port = port;
            _accountService = accountService;
            Logger = NullLogger<Peer>.Instance;

            _client = client;

            _messageReader = reader;
            _messageWriter = writer;

            CurrentHeight = currentHeight;
        }

        private void SetupHeartbeat()
        {
            _pingPongTimer.Interval = DefaultPingInterval;
            _pingPongTimer.Elapsed += TimerTimeoutElapsed;
            _pingPongTimer.AutoReset = true;
        }

        public bool Start()
        {
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(Peer), "This peer as already been disposed.");

            if (IsAuthentified)
                throw new InvalidOperationException("Cannot start an already authentified peer.");

            if (_messageReader == null || _messageWriter == null || _client == null)
                throw new InvalidOperationException("Could not initialize, null components.");

            try
            {
                _messageReader.PacketReceived += ClientOnPacketReceived;
                _messageReader.StreamClosed += MessageReaderOnStreamClosed;

                _messageReader.Start();
                _messageWriter.Start();

                StartAuthentification();
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error while initializing the connection.");
                Dispose();
                return false;
            }

            return true;
        }

        private void MessageReaderOnStreamClosed(object sender, EventArgs eventArgs)
        {
            Dispose();

            Logger.LogWarning($"Peer connection has been terminated : {DistantNodeData}.");

            PeerDisconnected?.Invoke(this,
                new PeerDisconnectedArgs {Peer = this, Reason = DisconnectReason.StreamClosed});
        }

        private void ClientOnPacketReceived(object sender, EventArgs eventArgs)
        {
            if (IsDisposed)
                return;

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
                    Logger.LogWarning($"Received message while not authentified: {a.Message}.");
                    return;
                }

                switch (a.Message.Type)
                {
                    case (int) MessageType.Ping:
                        HandlePingMessage(a.Message);
                        return;
                    case (int) MessageType.Pong:
                        HandlePongMessage(a.Message);
                        return;
                }

                PacketsReceivedCount++;

                FireMessageReceived(a.Message);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Exception while handle received packet.");
            }
        }

        #region Authentification

        /// <summary>
        /// This method sends authentification information to the distant peer and
        /// start the authentification timer.
        /// </summary>
        /// <returns></returns>
        private void StartAuthentification()
        {
            try
            {
                var nodeInfo = new NodeData {Port = _port};

                var publicKey = _accountService.GetPublicKeyAsync().Result;
                var signature = _accountService.SignAsync(SHA256.Create().ComputeHash(nodeInfo.ToByteArray())).Result;

                var nd = new Handshake
                {
                    NodeInfo = nodeInfo,
                    PublicKey = ByteString.CopyFrom(publicKey),
                    Height = CurrentHeight,
                    Sig = ByteString.CopyFrom(signature),
                    Version = GlobalConfig.ProtocolVersion,
                };

                if (publicKey == null)
                    Logger.LogWarning("Node public key is null.");

                byte[] packet = nd.ToByteArray();

                Logger.LogTrace(
                    $"Sending authentification : {{ port: {nd.NodeInfo.Port}, addr: {nd.PublicKey.ToByteArray().ToHex()}, height: {nd.Height}, version {nd.Version} }}");

                _messageWriter.EnqueueMessage(new Message
                    {Type = (int) MessageType.Auth, HasId = false, Length = packet.Length, Payload = packet});

                StartAuthTimer();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private void StartAuthTimer()
        {
            _authTimer.Interval = AuthTimeout;
            _authTimer.Elapsed += AuthTimerElapsed;
            _authTimer.AutoReset = false;
            _authTimer.Start();
        }

        private void AuthTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _authTimer.Stop(); // dispose

            if (IsAuthentified)
                return;

            Logger.LogWarning("Authentification timed out.");

            Dispose();

            AuthFinished?.Invoke(this, new AuthFinishedArgs(RejectReason.AuthTimeout));
        }

        /// <summary>
        /// Handles authentification information.
        /// </summary>
        /// <param name="aMessage"></param>
        private void HandleAuthResponse(Message aMessage)
        {
            try
            {
                _authTimer.Stop();

                Handshake handshk = Handshake.Parser.ParseFrom(aMessage.Payload);

                AuthentifyWith(handshk);

                // Update with the real IP address
                IPEndPoint remoteEndPoint = (IPEndPoint) _client.Client.RemoteEndPoint;
                handshk.NodeInfo.IpAddress = remoteEndPoint.Address.ToString();

                KnownHeight = handshk.Height;

                _pingPongTimer.Start();
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error processing authentification information.");
                Dispose();
            }

            AuthFinished?.Invoke(this, new AuthFinishedArgs());
        }

        /// <summary>
        /// Mainly for testing purposes, it's used for authentifying a node. Note that
        /// is doesn't launch the correponding event.
        /// </summary>
        /// <param name="handshakeMsg"></param>
        internal RejectReason AuthentifyWith(Handshake handshakeMsg)
        {
            if (handshakeMsg == null)
            {
                FireInvalidAuth(RejectReason.AuthInvalidHandshakeMsg);
                return RejectReason.AuthInvalidHandshakeMsg;
            }

            _lastReceivedHandshake = handshakeMsg;

            try
            {
                if (handshakeMsg.Version != GlobalConfig.ProtocolVersion)
                {
                    FireInvalidAuth(RejectReason.AuthWrongVersion);
                    return RejectReason.AuthWrongVersion;
                }

                DistantPubKey = handshakeMsg.PublicKey.ToByteArray();
                if (DistantPubKey == null)
                {
                    FireInvalidAuth(RejectReason.AuthInvalidKey);
                    return RejectReason.AuthInvalidKey;
                }

                DistantNodeAddress
                    = Address.FromPublicKey(DistantPublicKey).GetFormatted();

                // verify sig
                bool sigValid = _accountService.VerifySignatureAsync(handshakeMsg.Sig.ToByteArray(), SHA256.Create()
                    .ComputeHash(handshakeMsg.NodeInfo.ToByteArray()), DistantPubKey).Result;

                if (!sigValid)
                {
                    FireInvalidAuth(RejectReason.AuthInvalidSig);
                    return RejectReason.AuthInvalidSig;
                }
            }
            catch (Exception)
            {
                FireInvalidAuth(RejectReason.AuthInvalidKey);
                return RejectReason.AuthInvalidKey;
            }

            IsAuthentified = true;
            return RejectReason.None;
        }

        private void FireInvalidAuth(RejectReason reason)
        {
            AuthFinished?.Invoke(this, new AuthFinishedArgs(reason));
        }

        #endregion Authentification

        private void FireMessageReceived(Message p)
        {
            MessageReceived?.Invoke(this, new PeerMessageReceivedArgs {Peer = this, Message = p});
        }

        /// <summary>
        /// Sends the provided message to the peer.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="successCallback"></param>
        /// <returns></returns>
        public void EnqueueOutgoing(Message msg, Action<Message> successCallback = null)
        {
            try
            {
                if (!IsAuthentified)
                {
                    Logger.LogWarning($"Can't write : not identified {DistantNodeData}.");
                }

                if (_messageWriter == null)
                {
                    Logger.LogWarning(
                        $"Peer {DistantNodeData?.IpAddress} : {DistantNodeData?.Port} - Null stream while sending");
                    return;
                }

                _messageWriter.EnqueueMessage(msg, successCallback);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Exception while sending data.");
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

        public override int GetHashCode()
        {
            var hash = 1;
            if (IpAddress.Length != 0) hash ^= IpAddress.GetHashCode();
            if (Port != 0) hash ^= Port.GetHashCode();
            return hash;
        }

        #region Closing and disposing

        public void Dispose()
        {
            if (IsDisposed)
                return;

            _pingPongTimer?.Stop();
            _authTimer?.Stop();

            if (_messageReader != null)
            {
                _messageReader.PacketReceived -= ClientOnPacketReceived;
                _messageReader.StreamClosed -= MessageReaderOnStreamClosed;
            }

            foreach (var request in BlockRequests)
            {
                request.Cancel();
            }

            _messageReader?.Close();
            _messageWriter?.Close();

            _client?.Close();

            IsDisposed = true;
        }

        #endregion
    }
}