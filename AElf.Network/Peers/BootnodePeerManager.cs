using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Network.Config;
using AElf.Network.Data;
using AElf.Network.Peers.Exceptions;
using NLog;

namespace AElf.Network.Peers
{
    public class BootnodePeerManager: IPeerManager, IDisposable
    {
        public event EventHandler MessageReceived;
        public event EventHandler PeerListEmpty;
        
        public event EventHandler PeerAdded;
        public event EventHandler PeerRemoved;

        private INodeDialer _nodeDialer;
        private IAElfNetworkConfig _networkConfig;
        private ILogger _logger;
        private IAElfServer _server;
        private NodeData _nodeData;

        public bool UndergoingPm { get; private set; } = false;
        public bool AddingPeer { get; private set; } = false;
        
        // List of bootnodes that the manager was started with
        private readonly List<NodeData> _bootnodes = new List<NodeData>();
        
        // List of connected bootnodes
        private readonly List<IPeer> _bootnodePeers = new List<IPeer>();
        
        // List of non bootnode peers
        private readonly List<IPeer> _peers = new List<IPeer>();
        
        private Timer _maintenanceTimer = null;
        private readonly TimeSpan _initialMaintenanceDelay = TimeSpan.FromSeconds(5);
        private readonly TimeSpan _maintenancePeriod = TimeSpan.FromSeconds(10);
        
        public bool NoPeers { get; }

        public BootnodePeerManager(IAElfServer server, IAElfNetworkConfig config, 
            INodeDialer nodeDialer, ILogger logger)
        {
            _nodeDialer = nodeDialer;
            _networkConfig = config;
            _logger = logger;
            _server = server;

            if (_networkConfig != null)
            {
                // Set up the data that describes this node 
                _nodeData = new NodeData { IpAddress = _networkConfig.Host, Port = _networkConfig.Port };

                // Check to see if this node *is* any of the provided bootnodes
                _nodeData.IsBootnode = _networkConfig?.Bootnodes?.Any(p => p.Equals(_nodeData)) ?? false;

                if (_networkConfig.Bootnodes != null)
                {
                    // Paranoid check : make sure that all the bootnodes IsBootnode property is set properly
                    // most of the code relies on this to work properly
                    foreach (var node in _networkConfig.Bootnodes.Where(p => !p.Equals(_nodeData)))
                    {
                        node.IsBootnode = true;
                        _bootnodes.Add(node);
                    }
                }
            }
        }
        
        public void Start()
        {
            Task.Run(() => _server.StartAsync());
            //Setup();
            
            _server.ClientConnected += HandleConnection;
            
            _maintenanceTimer = new Timer(async e => await DoPeerMaintenance(), null, _initialMaintenanceDelay, _maintenancePeriod);
        }
        
        private void HandleConnection(object sender, EventArgs e)
        {
            if (sender != null && e is ClientConnectedArgs args)
            {
                AddPeer(args.NewPeer);
            }
        }
        
        /// <summary>
        /// At a specified period this callback is executed to ensure the proper state of
        /// the manager. For a boot node the main task is to ensure connection to the
        /// other bootnodes. 
        /// </summary>
        internal async Task DoPeerMaintenance()
        {
            if (_peers == null)
                return;
            
            // If we're already in a maintenance cycle or adding a peer: do nothing
            if (UndergoingPm || AddingPeer)
                return;
            
            UndergoingPm = true;
            
            if (_bootnodePeers.Count != _bootnodes.Count)
            {
                // If this bootnode is not connected to one of the others 
                await ConnectBootnodes();
            }

            UndergoingPm = false;
        }
        
        internal async Task ConnectBootnodes()
        {
            // Get the currently connected bootnodes
            List<NodeData> current = _bootnodePeers.Select(b => b.DistantNodeData).ToList();
            List<NodeData> toAdd = _bootnodes.Where(b => !current.Contains(b)).ToList();
                
            foreach (var bootNode in toAdd)
            {
                IPeer bootnode = await DialNode(bootNode);
                
                // If we successfully connected to the other peer
                // add it to be managed
                if (bootnode != null)
                {
                    _bootnodePeers.Add(bootnode);
                    _logger?.Trace("Successfuly dialed bootnode: " + bootnode);

                    bootnode.PeerDisconnected += BootnodeDisconnected;
                }
            }
        }

        /// <summary>
        /// Callback for when a Peer fires a <see cref="PeerDisconnected"/> event. It unsubscribes
        /// the manager from the events and removes it from the list.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BootnodeDisconnected(object sender, EventArgs e)
        {
            if (sender != null && e is PeerDisconnectedArgs args && args.Peer != null)
            {
                IPeer peer = args.Peer;
                
                peer.PeerDisconnected -= BootnodeDisconnected;
                
                _bootnodePeers.Remove(peer);
                
                _logger?.Trace("Successfuly removed bootnode: " + peer);
            }
        }

        public IPeer GetBootPeer(IPeer peer)
        {
            return _bootnodePeers?.FirstOrDefault(p => p.Equals(peer));
        }
        
        private async Task<IPeer> DialNode(NodeData nodeData)
        {
            if (nodeData == null)
                return null;
            
            try
            {
                return await _nodeDialer.DialAsync(nodeData);
            }
            catch (ResponseTimeOutException rex)
            {
                _logger?.Error(rex, rex?.Message + " - "  + nodeData);
            }

            return null;
        }
        
        /// <summary>
        /// Adds a peer to the manager and hooks up the callback for
        /// receiving messages from it. It also starts the peers
        /// listening process.
        /// </summary>
        /// <param name="peer">the peer to add</param>
        public bool AddPeer(IPeer peer)
        {
            if (UndergoingPm)
                return false;
            
            if (peer == null)
                return false;

            AddingPeer = true;
            
            peer.DistantNodeData.IsBootnode = _networkConfig?.Bootnodes?.Any(p => p.Equals(_nodeData)) ?? false;

            if (peer.IsBootnode && GetBootPeer(peer) == null)
            {
                _bootnodePeers.Add(peer);
                _logger?.Trace("[AddPeer] Bootnode added : " + peer);
                peer.PeerDisconnected += BootnodeDisconnected;
            }

            // Don't add a peer already in the list
            /*if (GetPeer(peer) != null)
                return false;*/
            
            //_peers.Add(peer);
            
            //peer.MessageReceived += ProcessPeerMessage;
            //peer.PeerDisconnected += ProcessClientDisconnection;
            
            Task.Run(peer.StartListeningAsync);

            AddingPeer = false;

            return true;
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

        public List<IPeer> GetPeers()
        {
            throw new NotImplementedException();
        }

        public List<NodeData> GetPeers(ushort? numPeers, bool includeBootnodes = true)
        {
            throw new NotImplementedException();
        }

        public Task<int> BroadcastMessage(MessageTypes messageType, byte[] payload, int requestId)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}