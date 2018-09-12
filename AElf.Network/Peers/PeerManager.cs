using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AElf.Common.Attributes;
using AElf.Common.ByteArrayHelpers;
using AElf.Common.Extensions;
using AElf.Configuration;
using AElf.Configuration.Config.Network;
using AElf.Network.Connection;
using AElf.Network.Data;
using AElf.Network.Eventing;
using Google.Protobuf;
using Newtonsoft.Json.Linq;
using NLog;

[assembly:InternalsVisibleTo("AElf.Network.Tests")]
namespace AElf.Network.Peers
{
    internal enum PeerManagerJobType { DialNode, ProcessMessage }
    
    internal class PeerManagerJob
    {
        public PeerManagerJobType Type { get; set; }
        public NodeData Node { get; set; }
        public PeerMessageReceivedArgs Message { get; set; }
    }
    
    [LoggerName(nameof(PeerManager))]
    public class PeerManager : IPeerManager
    {
        public event EventHandler PeerEvent;
                
        public const int TargetPeerCount = 8;
        
        private readonly ILogger _logger;
        private readonly IConnectionListener _connectionListener;
        
        private System.Threading.Timer _maintenanceTimer;
        private readonly TimeSpan _initialMaintenanceDelay = TimeSpan.FromSeconds(10);
        private readonly TimeSpan _maintenancePeriod = TimeSpan.FromMinutes(1);
        
        private readonly List<IPeer> _authentifyingPeer = new List<IPeer>();
        private readonly List<IPeer> _peers = new List<IPeer>();
        
        private Object _peerListLock = new Object(); 
        
        private BlockingCollection<PeerManagerJob> _jobQueue;

        private AllowedConnection _allowedConnections = AllowedConnection.All;
        public List<string> _
        
        private readonly List<byte[]> _bpKeys;
        
        private byte[] _nodeKey;
        private string _nodeName;
        private bool _isBp;

        public PeerManager(IConnectionListener connectionListener, ILogger logger)
        {
            _jobQueue = new BlockingCollection<PeerManagerJob>();
            _bpKeys = new List<byte[]>();
            
            _connectionListener = connectionListener;
            _logger = logger;
            
            _nodeName = NodeConfig.Instance.NodeName;

            if (!string.IsNullOrWhiteSpace(NetworkConfig.Instance.NetAllowed))
            {
                if (Enum.TryParse(NetworkConfig.Instance.NetAllowed, out AllowedConnection myName))
                {
                    _allowedConnections = myName;
                }
            }

            //  todo bunch of pubkeys
            //            if (NetworkConfig.Instance.NetWhitelist != null)
            //            {
            //                foreach (var peer in NetworkConfig.Instance.NetWhitelist)
            //                {
            //                    if (peer.Length != "04fdf7d50f69be44a55a01f22d5910a96ddf")
            //                }
            //            }

            SetBpConfig();
        }
        
        private void SetBpConfig()
        {
            var producers = MinersConfig.Instance.Producers;
            
            // Set the list of block producers
            try
            {
                foreach (var bp in producers.Values)
                {
                    byte[] key = ByteArrayHelpers.FromHexString(bp["address"]);
                    _bpKeys.Add(key);
                }
            }
            catch (Exception e)
            {
                _logger?.Warn(e, "Error while reading mining info.");
            }
            
            // This nodes key
            _nodeKey = ByteArrayHelpers.FromHexString(NodeConfig.Instance.NodeAccount);
            _isBp = _bpKeys.Any(k => k.BytesEqual(_nodeKey));
        }

        public void Start()
        {
            Task.Run(() => _connectionListener.StartListening(NetworkConfig.Instance.ListeningPort));

            _connectionListener.IncomingConnection += OnIncomingConnection;
            _connectionListener.ListeningStopped += OnListeningStopped;

            if (!_isBp)
                _maintenanceTimer = new System.Threading.Timer(e => DoPeerMaintenance(), null, _initialMaintenanceDelay, _maintenancePeriod);

            // Add the provided bootnodes
            if (NetworkConfig.Instance.Bootnodes != null && NetworkConfig.Instance.Bootnodes.Any())
            {
                // todo add jobs
                foreach (var btn in NetworkConfig.Instance.Bootnodes)
                {
                    NodeData nd = NodeData.FromString(btn);
                    var dialJob = new PeerManagerJob {Type = PeerManagerJobType.DialNode, Node = nd};
                    _jobQueue.Add(dialJob);
                }
            }
            else
            {
                _logger?.Warn("Bootnode list is empty.");
            }

            Task.Run(() => StartProcessing()).ConfigureAwait(false);
        }

        public async Task<JObject> GetPeers()
        {
            List<NodeData> pl = _peers.Select(p => p.DistantNodeData).ToList();
            
            PeerListData pldata = new PeerListData();
            foreach (var peer in pl)
            {
                pldata.NodeData.Add(peer);
            }

             JObject peers = JObject.Parse(JsonFormatter.Default.Format(pldata));
            peers["auth"] = _authentifyingPeer.Count;
            
            return peers;
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
                        _logger?.Warn(e, "Error while dequeuing peer manager job: stopping the dequeing loop.");
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
                    _logger?.Warn(e, "Exception while dequeuing job.");
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
                _logger?.Warn("Data is null, cannot add peer.");
                return;
            }
            
            NodeDialer dialer = new NodeDialer(nodeData.IpAddress, nodeData.Port);
            TcpClient client = dialer.DialAsync().GetAwaiter().GetResult();

            if (client == null)
            {
                _logger?.Warn($"Could not connect to {nodeData.IpAddress}:{nodeData.Port}, operation timed out.");
                return;
            }

            IPeer peer;
            
            try
            {
                peer = CreatePeerFromConnection(client);
            }
            catch (Exception e)
            {
                _logger?.Warn(e, "Creation of peer object failed");
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
            
            IPeer peer = new Peer(client, reader, writer, NetworkConfig.Instance.ListeningPort, _nodeKey);
            
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
            if (sender is Peer peer)
            {
                peer.IsBp = peer.DistantNodeAddress != null && _bpKeys.Any(k => k.BytesEqual(peer.DistantNodeAddress)); 
                AddAuthentifiedPeer(peer);
            }
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
                _logger?.Warn("Peer is null, cannot add.");
                return;
            }
            
            if (!peer.IsAuthentified)
            {
                _logger?.Warn($"Peer not authentified, cannot add {peer}");
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
                
            _logger?.Debug($"Peer authentified and added : {{ addr: {peer}, key: {peer.DistantNodeAddress.ToHex() }, bp: {peer.IsBp} }}");
            
            peer.MessageReceived += OnPeerMessageReceived;
                
            PeerEvent?.Invoke(this, new PeerEventArgs(peer, PeerEventType.Added));
        }

        private void OnPeerMessageReceived(object sender, EventArgs args)
        {
            if (sender != null && args is PeerMessageReceivedArgs peerMsgArgs && peerMsgArgs.Message is Message msg) 
            {
                if (msg.Type == (int)MessageType.RequestPeers || msg.Type == (int)MessageType.Peers)
                {
                    try
                    {
                        _jobQueue.Add(new PeerManagerJob { Type = PeerManagerJobType.ProcessMessage, Message = peerMsgArgs });
                    }
                    catch (Exception ex)
                    {
                        _logger?.Warn(ex, "Error while enqueuing job.");
                    }
                }
            }
            else
            {
                if (sender is IPeer peer)
                    _logger?.Warn($"Received an invalid message from {peer.DistantNodeData}.");
                else
                    _logger?.Warn("Received an invalid message.");
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
            {
                IPeer peer = args.Peer;
                
                peer.MessageReceived -= OnPeerMessageReceived;
                peer.PeerDisconnected -= ProcessClientDisconnection;
                peer.AuthFinished -= PeerOnPeerAuthentified;

                _authentifyingPeer.Remove(args.Peer);

                if (_peers.Remove(args.Peer))
                {
                    PeerEvent?.Invoke(this, new PeerEventArgs(peer, PeerEventType.Removed));
                }
                else
                {
                    _logger?.Warn($"Tried to remove peer, but not in list {args.Peer}");
                }
            }
        }
        
        #endregion Peer disconnection

        #region Peer maintenance

        private void HandlePeerRequestMessage(PeerMessageReceivedArgs args)
        {
            try
            {
                _logger?.Debug($"Received peer request from {args.Peer}.");
                
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
                    _logger?.Debug("No peers to return.");
                    return;
                }

                byte[] payload = pListData.ToByteArray();
                var resp = new Message
                {
                    Type = (int)MessageType.Peers,
                    Length = payload.Length,
                    Payload = payload
                };
                        
                _logger?.Debug($"Sending peers : {pListData} to {args.Peer}");

                Task.Run(() => args.Peer.EnqueueOutgoing(resp));
            }
            catch (Exception exception)
            {
                _logger?.Warn(exception, "Error while answering a peer request.");
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
                _logger?.Trace(str + peerStr);
                
                PeerListData peerList = PeerListData.Parser.ParseFrom(msg.Payload);
                _logger?.Trace($"Receiving peers - node list count {peerList.NodeData.Count}.");
                
                if (peerList.NodeData.Count > 0)
                    _logger?.Trace("Peers received : " + peerList.GetLoggerString());

                List<NodeData> currentPeers;
                lock (_peerListLock)
                {
                    currentPeers = _peers.Select(p => p.DistantNodeData).ToList();
                }

                foreach (var peer in peerList.NodeData.Where(nd => !currentPeers.Contains(nd)))
                {
                    _jobQueue.Add(new PeerManagerJob { Type = PeerManagerJobType.DialNode, Node = peer });
                }
            }
            catch (Exception e)
            {
                _logger?.Warn(e, "Invalid peer(s) - Could not receive peer(s) from the network", null);
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
                _logger?.Warn(e, "Error while doing peer maintenance.");
            }
        }
        
        #endregion

        #region Incoming connections

        private void OnListeningStopped(object sender, EventArgs eventArgs)
        {
            _logger.Warn("Listening stopped");
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
                    _logger?.Warn(e, "Creation of peer object failed");
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
                _logger?.Warn(e, "Error while sending a message to the peers.");
            }

            return count;
        }
    }
}