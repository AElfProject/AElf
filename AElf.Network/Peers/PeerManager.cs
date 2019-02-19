﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Common;
using AElf.Configuration;
using AElf.Cryptography.ECDSA;
using AElf.Kernel.Account;
using AElf.Network.Connection;
using AElf.Network.Data;
using AElf.Network.Eventing;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Volo.Abp.DependencyInjection;

[assembly: InternalsVisibleTo("AElf.Network.Tests")]

namespace AElf.Network.Peers
{
    internal enum PeerManagerJobType
    {
        DialNode,
        ProcessMessage
    }

    internal class PeerManagerJob
    {
        public PeerManagerJobType Type { get; set; }
        public NodeData Node { get; set; }
        public PeerMessageReceivedArgs Message { get; set; }
    }

    
    public class PeerManager : IPeerManager, ISingletonDependency
    {
        public event EventHandler PeerEvent;

        public const int TargetPeerCount = 8;

        public ILogger<PeerManager> Logger {get;set;}
        private readonly IConnectionListener _connectionListener;

        private readonly IChainService _chainService;
        //private readonly IBlockChain _blockChain;

        private Timer _maintenanceTimer;
        private readonly TimeSpan _initialMaintenanceDelay = TimeSpan.FromSeconds(10);
        private readonly TimeSpan _maintenancePeriod = TimeSpan.FromMinutes(1);

        private readonly List<IPeer> _authentifyingPeer = new List<IPeer>();
        private readonly List<IPeer> _peers = new List<IPeer>();

        private Object _peerListLock = new Object();

        private BlockingCollection<PeerManagerJob> _jobQueue;

        private AllowedConnection _allowedConnections = AllowedConnection.All;
        private List<byte[]> _whiteList;

        // Temp solution until the BP voting gets implemented
        internal readonly List<string> _bpAddresses;
        internal bool _isBp;

        private readonly NetworkOptions _networkOptions;
        private readonly IAccountService _accountService;

        public PeerManager(IConnectionListener connectionListener, IChainService chainService,
            IOptionsSnapshot<NetworkOptions> options, IAccountService accountService)
        {
            _jobQueue = new BlockingCollection<PeerManagerJob>();
            _bpAddresses = new List<string>();
            _whiteList = new List<byte[]>();

            _connectionListener = connectionListener;
            _chainService = chainService;
            _networkOptions = options.Value;
            _accountService = accountService;
            //_blockChain = blockChain;
            Logger = NullLogger<PeerManager>.Instance;

            if (!string.IsNullOrWhiteSpace(_networkOptions.NetAllowed))
            {
                if (Enum.TryParse(_networkOptions.NetAllowed, out AllowedConnection myName))
                {
                    _allowedConnections = myName;
                }
            }

            if (_networkOptions.NetWhitelist != null)
            {
                foreach (var peer in _networkOptions.NetWhitelist)
                {
                    _whiteList.Add(ByteArrayHelpers.FromHexString(peer));
                }
            }

            SetBpConfig();
        }

        private void SetBpConfig()
        {
            var account = _accountService.GetAccountAsync().Result;

            // This nodes key
            var thisAddr = account.GetFormatted();
            _isBp = _bpAddresses.Any(k => k.Equals(thisAddr));
        }

        public void Start()
        {
            Task.Run(() => _connectionListener.StartListening(_networkOptions.ListeningPort));

            _connectionListener.IncomingConnection += OnIncomingConnection;
            _connectionListener.ListeningStopped += OnListeningStopped;

            if (!_isBp)
                _maintenanceTimer = new Timer(e => DoPeerMaintenance(), null, _initialMaintenanceDelay, _maintenancePeriod);

            // Add the provided boot nodes
            if (_networkOptions.BootNodes != null && _networkOptions.BootNodes.Any())
            {
                // todo add jobs
                foreach (var btn in _networkOptions.BootNodes)
                {
                    NodeData nd = NodeData.FromString(btn);
                    var dialJob = new PeerManagerJob {Type = PeerManagerJobType.DialNode, Node = nd};
                    _jobQueue.Add(dialJob);
                }
            }
            else
            {
                Logger.LogWarning("Bootnode list is empty.");
            }

            Task.Run(() => StartProcessing()).ConfigureAwait(false);
        }

        public Task Stop()
        {
            return Task.CompletedTask;
        }

        public Task<JObject> GetPeers()
        {
            var peers = new JObject
            {
                ["Auth"] = _authentifyingPeer.Count
            };
            if (_peers.Count > 0)
            {
                peers["Peers"] = JArray.Parse(JsonConvert.SerializeObject(_peers));
            }

            return Task.FromResult(peers);
        }

        private void StartProcessing()
        {
            while (true)
            {
                try
                {
                    PeerManagerJob job = null;

                    try
                    {
                        job = _jobQueue.Take();
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, "Error while dequeuing peer manager job: stopping the dequeing loop.");
                        break;
                    }

                    if (job.Type == PeerManagerJobType.ProcessMessage)
                    {
                        HandleMessage(job.Message);
                    }
                    else if (job.Type == PeerManagerJobType.DialNode)
                    {
                        AddPeer(job.Node);
                    }
                }
                catch (Exception e)
                {
                    Logger.LogWarning(e, "Exception while dequeuing job.");
                }
            }
        }

        private void HandleMessage(PeerMessageReceivedArgs args)
        {
            if (args.Message != null)
            {
                if (args.Message.Type == (int) MessageType.RequestPeers)
                {
                    HandlePeerRequestMessage(args);
                }
                else if (args.Message.Type == (int) MessageType.Peers)
                {
                    ReceivePeers(args.Peer, args.Message);
                }
            }
        }

        #region Peer creation

        /// <summary>
        /// Adds a peer to the peer management. This method will initiate the
        /// authentification procedure.
        /// </summary>
        /// <param name="nodeData"></param>
        public void AddPeer(NodeData nodeData)
        {
            if (nodeData == null)
            {
                Logger.LogWarning("Data is null, cannot add peer.");
                return;
            }

            NodeDialer dialer = new NodeDialer(nodeData.IpAddress, nodeData.Port);
            TcpClient client = dialer.DialAsync().GetAwaiter().GetResult();

            if (client == null)
            {
                Logger.LogWarning($"Could not connect to {nodeData.IpAddress}:{nodeData.Port}, operation timed out.");
                return;
            }

            IPeer peer;

            try
            {
                peer = CreatePeerFromConnection(client);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Creation of peer object failed");
                return;
            }

            peer.PeerDisconnected += ProcessClientDisconnection;

            StartAuthentification(peer);
        }

        /// <summary>
        /// Given a client, this method will create a peer with the appropriate
        /// stream writer and reader. 
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        private IPeer CreatePeerFromConnection(TcpClient client)
        {
            NetworkStream nsStream = client?.GetStream();

            if (nsStream == null)
                throw new ArgumentNullException(nameof(client), "The client or its stream was null.");

            MessageReader reader = new MessageReader(nsStream);
            MessageWriter writer = new MessageWriter(nsStream);

            int height = (int) _chainService.GetBlockChain(0).GetCurrentBlockHeightAsync().Result;

            IPeer peer = new Peer(client, reader, writer, _networkOptions.ListeningPort, height,_accountService);

            return peer;
        }

        internal void StartAuthentification(IPeer peer)
        {
            lock (_peerListLock)
            {
                _authentifyingPeer.Add(peer);
            }

            peer.AuthFinished += PeerOnPeerAuthentified;

            // Start and authentify the peer
            peer.Start();
        }

        private void PeerOnPeerAuthentified(object sender, EventArgs eventArgs)
        {
            if (!(sender is Peer peer) || !(eventArgs is AuthFinishedArgs authArgs))
                return;

            if (!authArgs.IsAuthentified)
            {
                Logger.LogWarning($"Peer {peer} not authentified, reason : {authArgs.Reason}.");
                RemovePeer(peer);
                return;
            }

            peer.IsBp = peer.DistantNodeAddress != null && _bpAddresses.Any(k => k.Equals(peer.DistantNodeAddress));

            switch (_allowedConnections)
            {
                case AllowedConnection.BPs when !peer.IsBp:
                {
                    Logger.LogWarning($"Only producers are allowed to connect. Rejecting {peer}.");
                    RemovePeer(peer);
                    return;
                }
                case AllowedConnection.Listed:
                case AllowedConnection.BPsAndListed:
                {
                    string pub = peer.DistantNodeAddress;
                    bool inWhiteList = _whiteList.Any(p => p.Equals(pub));

                    if (_allowedConnections == AllowedConnection.Listed && !inWhiteList)
                    {
                        Logger.LogWarning($"Only listed peers are allowed to connect. Rejecting {peer}.");
                        RemovePeer(peer);
                        return;
                    }

                    if (_allowedConnections == AllowedConnection.BPsAndListed && !inWhiteList && !peer.IsBp)
                    {
                        Logger.LogWarning($"Only listed peers or bps are allowed to connect. Rejecting {peer}.");
                        RemovePeer(peer);
                        return;
                    }

                    break;
                }
            }

            AddAuthentifiedPeer(peer);
        }

        /// <summary>
        /// Returns the first occurence of the peer. IPeer
        /// implementations may override the equality logic.
        /// </summary>
        /// <param name="peer"></param>
        /// <returns></returns>
        public IPeer GetPeer(IPeer peer)
        {
            return _peers?.FirstOrDefault(p => p.Equals(peer));
        }

        internal void AddAuthentifiedPeer(IPeer peer)
        {
            if (peer == null)
            {
                Logger.LogWarning("Peer is null, cannot add.");
                return;
            }

            if (!peer.IsAuthentified)
            {
                Logger.LogWarning($"Peer not authentified, cannot add {peer}");
                return;
            }

            lock (_peerListLock)
            {
                _authentifyingPeer.Remove(peer);

                if (GetPeer(peer) != null)
                {
                    peer.Dispose(); // todo
                    return;
                }

                _peers.Add(peer);
            }

            Logger.LogInformation($"Peer authentified and added : {{ addr: {peer}, key: {peer.DistantNodeAddress},  bp: {peer.IsBp}, height: {peer.KnownHeight}}}");

            peer.MessageReceived += OnPeerMessageReceived;

            PeerEvent?.Invoke(this, new PeerEventArgs(peer, PeerEventType.Added));
        }

        private void OnPeerMessageReceived(object sender, EventArgs args)
        {
            if (sender != null && args is PeerMessageReceivedArgs peerMsgArgs && peerMsgArgs.Message is Message msg)
            {
                if (msg.Type == (int) MessageType.RequestPeers || msg.Type == (int) MessageType.Peers)
                {
                    try
                    {
                        _jobQueue.Add(new PeerManagerJob {Type = PeerManagerJobType.ProcessMessage, Message = peerMsgArgs});
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e, "Error while enqueuing job.");
                    }
                }
            }
            else
            {
                if (sender is IPeer peer)
                    Logger.LogWarning($"Received an invalid message from {peer.DistantNodeData}.");
                else
                    Logger.LogWarning("Received an invalid message.");
            }
        }

        #endregion Peer creation

        #region Peer diconnection

        /// <summary>
        /// Callback for when a Peer fires a <see cref="PeerDisconnected"/> event. It unsubscribes
        /// the manager from the events and removes it from the list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProcessClientDisconnection(object sender, EventArgs e)
        {
            if (sender != null && e is PeerDisconnectedArgs args && args.Peer != null)
                RemovePeer(args.Peer);
        }

        public void RemovePeer(NodeData nodeData)
        {
            IPeer peer = _peers.FirstOrDefault(p => p.IpAddress == nodeData.IpAddress && p.Port == nodeData.Port);
            RemovePeer(peer);
        }

        public void RemovePeer(IPeer peer)
        {
            try
            {
                if (peer == null)
                {
                    Logger.LogWarning("removing peer but peer is null.");
                    return;
                }

                // Will do nothing if already disposed
                peer.Dispose();

                peer.MessageReceived -= OnPeerMessageReceived;
                peer.PeerDisconnected -= ProcessClientDisconnection;
                peer.AuthFinished -= PeerOnPeerAuthentified;

                lock (_peerListLock)
                {
                    _authentifyingPeer.RemoveAll(p => p.IsDisposed);
                    _authentifyingPeer.Remove(peer);

                    if (_peers.Remove(peer))
                    {
                        Logger.LogDebug($"{peer} removed.");
                        PeerEvent?.Invoke(this, new PeerEventArgs(peer, PeerEventType.Removed));
                    }
                    else
                    {
                        Logger.LogWarning($"Tried to remove peer, but not in list {peer}");
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error while removing peer.");
            }
        }

        #endregion Peer disconnection

        #region Peer maintenance

        private void HandlePeerRequestMessage(PeerMessageReceivedArgs args)
        {
            try
            {
                Logger.LogDebug($"Received peer request from {args.Peer}.");

                ReqPeerListData req = ReqPeerListData.Parser.ParseFrom(args.Message.Payload);
                ushort numPeers = (ushort) req.NumPeers;

                PeerListData pListData = new PeerListData();

                // Filter the requestor out
                // Filter out BPs
                var peersToSend = _peers
                    .Where(p => p.DistantNodeData != null && !p.DistantNodeData.Equals(args.Peer.DistantNodeData))
                    .Where(p => !p.IsBp);

                foreach (var peer in peersToSend)
                {
                    pListData.NodeData.Add(peer.DistantNodeData);

                    if (pListData.NodeData.Count == numPeers)
                        break;
                }

                if (!pListData.NodeData.Any())
                {
                    Logger.LogDebug("No peers to return.");
                    return;
                }

                byte[] payload = pListData.ToByteArray();
                var resp = new Message
                {
                    Type = (int) MessageType.Peers,
                    Length = payload.Length,
                    Payload = payload
                };

                Logger.LogDebug($"Sending peers : {pListData} to {args.Peer}");

                Task.Run(() => args.Peer.EnqueueOutgoing(resp));
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error while answering a peer request.");
            }
        }

        /// <summary>
        /// This method processes the peers received from one of
        /// the connected peers.
        /// </summary>
        /// <param name="messagePayload"></param>
        /// <returns></returns>
        internal void ReceivePeers(IPeer pr, Message msg)
        {
            try
            {
                var str = $"Receiving peers from {pr} - current node list: \n";

                var peerStr = _peers.Select(c => c.ToString()).Aggregate((a, b) => a.ToString() + ", " + b);
                Logger.LogTrace(str + peerStr);

                PeerListData peerList = PeerListData.Parser.ParseFrom(msg.Payload);
                Logger.LogTrace($"Receiving peers - node list count {peerList.NodeData.Count}.");

                if (peerList.NodeData.Count > 0)
                    Logger.LogTrace("Peers received : " + peerList.GetLoggerString());

                List<NodeData> currentPeers;
                lock (_peerListLock)
                {
                    currentPeers = _peers.Select(p => p.DistantNodeData).ToList().Concat(_authentifyingPeer.Where(ap => !ap.IsDisposed).Select(ap => ap.DistantNodeData)).ToList();
                }

                foreach (var peer in peerList.NodeData.Where(nd => !currentPeers.Contains(nd)))
                {
                    _jobQueue.Add(new PeerManagerJob {Type = PeerManagerJobType.DialNode, Node = peer});
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Invalid peer(s) - Could not receive peer(s) from the network", null);
            }
        }

        #endregion

        #region Processing messages

        internal void DoPeerMaintenance()
        {
            if (_peers == null)
                return;

            try
            {
                int missingPeers = TargetPeerCount - _peers.Count;

                if (missingPeers > 0)
                {
                    var req = NetRequestFactory.CreateMissingPeersReq(missingPeers);
                    var taskAwaiter = BroadcastMessage(req);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error while doing peer maintenance.");
            }
        }

        #endregion

        #region Incoming connections

        private void OnListeningStopped(object sender, EventArgs eventArgs)
        {
            Logger.LogWarning("Listening stopped.");
        }

        private void OnIncomingConnection(object sender, EventArgs eventArgs)
        {
            if (sender != null && eventArgs is IncomingConnectionArgs args)
            {
                IPeer peer;

                try
                {
                    peer = CreatePeerFromConnection(args.Client);
                }
                catch (Exception e)
                {
                    Logger.LogError(e, "Creation of peer object failed");
                    return;
                }

                peer.PeerDisconnected += ProcessClientDisconnection;

                StartAuthentification(peer);
            }
        }

        #endregion Incoming connections

        #region Closing and disposing

        public void Dispose()
        {
            _maintenanceTimer?.Dispose();
        }

        #endregion Closing and disposing

        public int BroadcastMessage(Message message)
        {
            if (_peers == null || !_peers.Any())
                return 0;

            int count = 0;

            try
            {
                foreach (var peer in _peers)
                {
                    peer.EnqueueOutgoing(message);
                    count++;
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Error while sending a message to the peers.");
            }

            return count;
        }
    }
}