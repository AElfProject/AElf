using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using AElf.Network.Config;
using AElf.Network.Data;
using AElf.Network.Peers.Exceptions;
using Google.Protobuf;
using NLog;

[assembly:InternalsVisibleTo("AElf.Network.Tests")]
namespace AElf.Network.Peers
{
    public class PeerManager : IPeerManager, IDisposable
    {
        public const int TargetPeerCount = 8; 
        
        public event EventHandler MessageReceived;
        
        private readonly IAElfNetworkConfig _networkConfig;
        private readonly INodeDialer _nodeDialer;
        private readonly IAElfServer _server;
        private readonly IPeerDatabase _peerDatabase;
        private readonly ILogger _logger;

        private readonly List<NodeData> _bootnodes = new List<NodeData>();
        
        private readonly List<IPeer> _peers = new List<IPeer>();
        
        private readonly NodeData _nodeData;

        public bool UndergoingPm { get; private set; } = false;
        public bool ReceivingPeers { get; private set; } = false;

        public int BootnodeDropThreshold = TargetPeerCount / 2;

        private Timer _maintenanceTimer = null;
        private readonly TimeSpan _initialMaintenanceDelay = TimeSpan.FromSeconds(5);
        private readonly TimeSpan _maintenancePeriod = TimeSpan.FromMinutes(1);

        public PeerManager(IAElfServer server, IPeerDatabase peerDatabase, IAElfNetworkConfig config, 
            INodeDialer nodeDialer, ILogger logger)
        {
            _nodeDialer = nodeDialer;
            _networkConfig = config;
            _logger = logger;
            _server = server;
            _peerDatabase = peerDatabase;

            if (_networkConfig != null)
            {
                _nodeData = new NodeData()
                {
                    IpAddress = config.Host,
                    Port = config.Port
                };

                _nodeData.IsBootnode = _networkConfig?.Bootnodes?.Any(p => p.Equals(_nodeData)) ?? false;

                if (_networkConfig.Bootnodes != null)
                {
                    foreach (var node in _networkConfig.Bootnodes.Where(p => !p.Equals(_nodeData)))
                    {
                        _bootnodes.Add(node);
                    }
                }
            }
        }
        
        private void HandleConnection(object sender, EventArgs e)
        {
            if (sender != null && e is ClientConnectedArgs args)
            {
                AddPeer(args.NewPeer);
            }
        }

        /// <summary>
        /// This method start the server that listens for incoming
        /// connections and sets up the manager.
        /// </summary>
        public void Start()
        {
            Task.Run(() => _server.StartAsync());
            Setup();
            
            _server.ClientConnected += HandleConnection;
        }

        /// <summary>
        /// Sets up the server according to the configuration that was
        /// provided.
        /// </summary>
        private async Task Setup()
        {
            if (_networkConfig == null)
                return;
            
            if (_networkConfig.Peers.Any())
            {
                foreach (var peer in _networkConfig.Peers)
                {
                    NodeData nodeData = NodeData.FromString(peer);
                    await CreateAndAddPeer(nodeData);
                }
            }

            var dbNodeData = _peerDatabase.ReadPeers();

            foreach (var p in dbNodeData)
            {
                await CreateAndAddPeer(p);
            }

            _maintenanceTimer = new Timer(e => DoPeerMaintenance(), null, _initialMaintenanceDelay, _maintenancePeriod);
        }
        
        internal void DoPeerMaintenance()
        {
            if (_peers == null)
                return;
            
            // If we're in the process of receiving peers (potentially modifiying _peers)
            // we return directly, we'll try again in the next cycle.
            if (ReceivingPeers)
                return;
            
            // If we're already in a maintenance cycle: do nothing
            if (UndergoingPm)
                return;
            
            UndergoingPm = true;

            // If there are no connected peers then try to connect to
            // the preferred bootnode. If that fails, try all other bootnodes
            if (_peers.Count == 0)
            {
                AddBootnode();
            } 
            else if (_peers.Count > BootnodeDropThreshold)
            {
                // Remove the first bootnode we find
                RemovePeer(_peers.FirstOrDefault(p => p.IsBootnode));
            }

            // After either the initial maintenance operation or the removal operation
            // (mutually exclusive) adjust the peers to get to TargetPeerCount.
            try
            {
                int missingPeers = TargetPeerCount - _peers.Count;
                
                if (missingPeers > 0)
                {
                    // We set UndergoingPm here because at this point it will be ok for 
                    // us to receive peers 
                    UndergoingPm = false;
                    
                    var req = NetRequestFactory.CreateMissingPeersReq(missingPeers);
                    var taskAwaiter = BroadcastMessage(req).GetAwaiter().GetResult();
                }
                else if (missingPeers < 0)
                {
                    // Here we will be modifying the _peers collection and we don't want
                    // anybody else modifying it.
                    
                    // Calculate peers to remove
                    List<IPeer> peersToRemove = GetPeersToRemove(Math.Abs(missingPeers));
                    
                    // Remove them
                    foreach (var peer in peersToRemove)
                       RemovePeer(peer);
                }
                else
                {
                    // Healthy peer list - nothing to do
                }
            }
            catch (Exception e)
            {
                ;
            }

            UndergoingPm = false;
        }

        /// <summary>
        /// Gets the peers to remove from the manager according to certain
        /// rules.
        /// todo : for now the rule is the first <see cref="count"/> peers
        /// </summary>
        /// <param name="count"></param>
        internal List<IPeer> GetPeersToRemove(int count)
        {
            // Calculate peers to remove
            List<IPeer> peersToRemove = _peers.Take(count).ToList();
            return peersToRemove;
        }

        internal void AddBootnode()
        {
            foreach (var bootNode in _bootnodes)
            {
                try
                {
                    // First bootnode we connect to we add it, for now we add only one
                    if (CreateAndAddPeer(bootNode).GetAwaiter().GetResult() != null)
                        break;
                }
                catch(Exception e)
                {
                    ;
                }
            }
        }
        
        /// <summary>
        /// This method processes the peers received from one of
        /// the connected peers.
        /// </summary>
        /// <param name="messagePayload"></param>
        /// <returns></returns>
        internal async Task ReceivePeers(ByteString messagePayload)
        {
            // If we're in a maintenance cycle - do nothing
            // todo : maybe later we can queue this work...
            if (UndergoingPm)
                return;
                
            ReceivingPeers = true;
            
            try
            {
                PeerListData peerList = PeerListData.Parser.ParseFrom(messagePayload);
                
                _logger?.Trace("Peers received : " + peerList.GetLoggerString());

                foreach (var peer in peerList.NodeData)
                {
                    NodeData p = new NodeData { IpAddress = peer.IpAddress, Port = peer.Port };
                    IPeer newPeer = await CreateAndAddPeer(p);
                }
            }
            catch (Exception e)
            {
                ReceivingPeers = false;
                _logger?.Error(e, "Invalid peer(s) - Could not receive peer(s) from the network", null);
            }

            ReceivingPeers = false;
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

        /// <summary>
        /// Adds a peer to the manager and hooks up the callback for
        /// receiving messages from it. It also starts the peers
        /// listening process.
        /// </summary>
        /// <param name="peer">the peer to add</param>
        public void AddPeer(IPeer peer)
        {
            // Don't add a peer already in the list
            if (GetPeer(peer) != null)
                return;
            
            _peers.Add(peer);
            
            peer.MessageReceived += ProcessPeerMessage;
            peer.PeerDisconnected += ProcessClientDisconnection;

            _logger?.Trace("Peer added : " + peer);

            Task.Run(peer.StartListeningAsync);
        }
        
        /// <summary>
        /// Creates a Peer.
        /// </summary>
        /// <param name="nodeData"></param>
        /// <returns></returns>
        private async Task<IPeer> CreateAndAddPeer(NodeData nodeData)
        {
            if (nodeData == null)
                return null;
            
            //IPeer peer = new Peer(_nodeData, nodeData);
            
            
            try
            {
                //bool success = await peer.DoConnectAsync();
                
                IPeer peer = await _nodeDialer.DialAsync(nodeData);
                
                // If we successfully connected to the other peer
                // add it to be managed
                if (peer != null)
                {
                    AddPeer(peer);
                    return peer;
                }
            }
            catch (ResponseTimeOutException rex)
            {
                _logger?.Error(rex, rex?.Message + " - "  + nodeData);
            }

            return null;
        }
        
        /// <summary>
        /// Removes a peer from the list of peers.
        /// </summary>
        /// <param name="peer">the peer to remove</param>
        public void RemovePeer(IPeer peer)
        {
            _peers.Remove(peer);
            _logger?.Trace("Peer removed : " + peer);
        }

        public void RemovePeer(NodeData nodeData)
        {
            foreach (var peer in _peers)
            {
                if (peer.IpAddress == nodeData.IpAddress && peer.Port == nodeData.Port)
                {
                    _peers.Remove(peer);
                    _logger?.Trace("Peer Removed : " + peer);
                    break;
                }
            }
        }

        /// <summary>
        /// Returns a specified number of random peers from the peer
        /// list.
        /// </summary>
        /// <param name="numPeers">number of peers requested</param>
        public List<NodeData> GetPeers(ushort numPeers)
        {
            List<NodeData> peers = new List<NodeData>();
            
            for (ushort i = 0; i < numPeers - 1; i++)
            {
                if (i <= _peers.Count)
                {
                    NodeData p = new NodeData();
                    p.IpAddress = _peers[i].IpAddress;
                    p.Port = _peers[i].Port;
                    peers.Add(p);
                }
            }

            return peers;
        }
        
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
                args.Peer.MessageReceived -= ProcessPeerMessage;
                args.Peer.PeerDisconnected -= ProcessClientDisconnection;
                RemovePeer(args.Peer);
            }
        }

        private void ProcessPeerMessage(object sender, EventArgs e)
        {
            if (sender != null && e is MessageReceivedArgs args && args.Message != null)
            {
                if (args.Message.MsgType == (int) MessageTypes.RequestPeers)
                {
                    ReqPeerListData req = ReqPeerListData.Parser.ParseFrom(args.Message.Payload);
                    ushort numPeers = (ushort) req.NumPeers;
                    
                    PeerListData pListData = new PeerListData();

                    foreach (var peer in _peers.Where(p => !p.DistantNodeData.Equals(args.Peer.DistantNodeData)))
                    {
                        pListData.NodeData.Add(peer.DistantNodeData);
                        if (pListData.NodeData.Count == numPeers)
                            break;
                    }

                    var resp = new AElfPacketData
                    {
                        MsgType = (int)MessageTypes.ReturnPeers,
                        Length = 1,
                        Payload = pListData.ToByteString()
                    };

                    Task.Run(async () => await args.Peer.SendAsync(resp.ToByteArray()));
                }
                else if (args.Message.MsgType == (int) MessageTypes.ReturnPeers)
                {
                    ReceivePeers(args.Message.Payload);
                }
                else
                {
                    // raise the event so the higher levels can process it.
                    MessageReceived?.Invoke(this, e);
                }
            }
        }

        /// <summary>
        /// This message broadcasts data to all of its peers. This creates and
        /// sends a <see cref="AElfPacketData"/> object with the provided pay-
        /// load and message type.
        /// </summary>
        /// <param name="messageType"></param>
        /// <param name="payload"></param>
        /// <param name="messageId"></param>
        /// <returns></returns>
        public async Task<bool> BroadcastMessage(MessageTypes messageType, byte[] payload, int messageId)
        {
            if (_peers == null || !_peers.Any())
                return false;

            try
            {
                AElfPacketData packet = NetRequestFactory.CreateRequest(messageType, payload, messageId);
                bool success = await BroadcastMessage(packet);
                
                return success;
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Error while sending a message to the peers");
                return false;
            }
        }

        public async Task<bool> BroadcastMessage(AElfPacketData packet)
        {
            if (_peers == null || !_peers.Any())
                return false;

            try
            {
                byte[] data = packet.ToByteArray();

                foreach (var peer in _peers)
                {
                    await peer.SendAsync(data);
                }
            }
            catch (Exception e)
            {
                _logger?.Error(e, "Error while sending a message to the peers");
                throw;
            }

            return true;
        }

        public void Dispose()
        {
            _maintenanceTimer?.Dispose();
        }
    }
}