using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using AElf.Common.Attributes;
using AElf.Configuration.Config.Network;
using AElf.Network.Connection;
using AElf.Network.Data;
using AElf.RPC;
using Community.AspNetCore.JsonRpc;
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
        public event EventHandler PeerAdded;
                
        public const int TargetPeerCount = 8;
        
        private readonly ILogger _logger;
        private readonly IConnectionListener _connectionListener;
        
        private System.Threading.Timer _maintenanceTimer;
        private readonly TimeSpan _initialMaintenanceDelay = TimeSpan.FromSeconds(2);
        private readonly TimeSpan _maintenancePeriod = TimeSpan.FromMinutes(1);
        
        private readonly List<IPeer> _authentifyingPeer = new List<IPeer>();
        private readonly List<IPeer> _peers = new List<IPeer>();
        
        private Object _peerListLock = new Object(); 
        
        private BlockingCollection<PeerManagerJob> _jobQueue;

        public PeerManager(IConnectionListener connectionListener, ILogger logger)
        {
            _jobQueue = new BlockingCollection<PeerManagerJob>();
            _connectionListener = connectionListener;
            _logger = logger;
        }

        public void Start()
        {
            Task.Run(() => _connectionListener.StartListening(NetworkConfig.Instance.ListeningPort));

            _connectionListener.IncomingConnection += OnIncomingConnection;
            _connectionListener.ListeningStopped += OnListeningStopped;

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
                _logger?.Trace("Warning : bootnode list is empty.");
            }

            // todo consider removing the Peers option
            // todo exceptions

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
                        _logger?.Trace("Error while dequeuing peer manager job: stopping the dequeing loop.");
                        break;
                    }
                
                    // todo dispatch message

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
                    _logger?.Trace(e, "Exception while dequeuing job.");
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
                _logger?.Trace("Data is null, cannot add peer.");
                return; // todo exception
            }
            
            NodeDialer dialer = new NodeDialer(nodeData.IpAddress, nodeData.Port);
            TcpClient client = dialer.DialAsync().GetAwaiter().GetResult(); // todo async 

            if (client == null)
            {
                _logger?.Trace($"Could not connect to {nodeData.IpAddress}:{nodeData.Port}, operation timed out.");
                return;
            }
            
            IPeer peer = CreatePeerFromConnection(client);
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
            if (client == null)
                return null; // todo exception
            
            NetworkStream nsStream = client.GetStream();
            
            if (nsStream == null)
                return null; // todo log
            
            MessageReader reader = new MessageReader(nsStream);
            MessageWriter writer = new MessageWriter(nsStream);
            
            IPeer peer = new Peer(client, reader, writer, NetworkConfig.Instance.ListeningPort);
            
            return peer;
        }

        internal void StartAuthentification(IPeer peer)
        {
            // todo verification : must be connected

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
                // todo verify authentified
                // todo peer.MessageReceived += HandleNewMessage;
                // todo failed authentification

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
                _logger?.Trace("Peer is null, cannot add.");
                return;
            }
            
            if (!peer.IsAuthentified)
            {
                _logger?.Trace($"Peer not authentified, cannot add {peer}");
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
                
            _logger?.Trace($"Peer authentified and added : {peer}");
            
            peer.MessageReceived += OnPeerMessageReceived;
                
            PeerAdded?.Invoke(this, new PeerAddedEventArgs { Peer = peer });
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
                        _logger?.Trace(ex, "Error while enqueuing job.");
                    }
                }
            }
            else
            {
                if (sender is IPeer peer)
                    _logger?.Trace($"Received an invalid message from {peer.DistantNodeData}.");
                else
                    _logger?.Trace("Received an invalid message.");
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
                
                if (!_peers.Remove(args.Peer))
                    _logger?.Trace($"Tried to remove peer, but not in list {args.Peer}");
            }
        }
        
        #endregion Peer disconnection

        #region Peer maintenance

        private void HandlePeerRequestMessage(PeerMessageReceivedArgs args)
        {
            try
            {
                ReqPeerListData req = ReqPeerListData.Parser.ParseFrom(args.Message.Payload);
                ushort numPeers = (ushort) req.NumPeers;
                    
                PeerListData pListData = new PeerListData();

                foreach (var peer in _peers.Where(p => p.DistantNodeData != null && !p.DistantNodeData.Equals(args.Peer.DistantNodeData)))
                {
                    pListData.NodeData.Add(peer.DistantNodeData);
                            
                    if (pListData.NodeData.Count == numPeers)
                        break;
                }

                if (!pListData.NodeData.Any())
                    return;

                byte[] payload = pListData.ToByteArray();
                var resp = new Message
                {
                    Type = (int)MessageType.Peers,
                    Length = payload.Length,
                    Payload = payload,
                    OutboundTrace = Guid.NewGuid().ToString()
                };
                        
                _logger?.Trace($"Sending peers : {pListData} to {args.Peer}");

                Task.Run(() => args.Peer.EnqueueOutgoing(resp));
            }
            catch (Exception exception)
            {
                _logger?.Trace(exception, "Error while answering a peer request.");
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
                // todo should add ?
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
                _logger?.Error(e, "Invalid peer(s) - Could not receive peer(s) from the network", null);
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
                _logger?.Trace(e, "Error while doing peer maintenance.");
            }
        }
        
        #endregion

        #region Incoming connections

        private void OnListeningStopped(object sender, EventArgs eventArgs)
        {
            _logger.Trace("Listening stopped"); // todo
        }
        
        private void OnIncomingConnection(object sender, EventArgs eventArgs)
        {
            if (sender != null && eventArgs is IncomingConnectionArgs args)
            {
                IPeer peer = CreatePeerFromConnection(args.Client);
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
        
        
        // todo remove duplicate

        public int BroadcastMessage(Message message)
        {
            if (_peers == null || !_peers.Any())
                return 0;

            int count = 0;
            
            try
            {
                foreach (var peer in _peers)
                {
                    try
                    {
                        peer.EnqueueOutgoing(message); //todo
                        count++;
                    }
                    catch (Exception e) { }
                }
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Error while sending a message to the peers.");
            }

            return count;
        }
    }
}