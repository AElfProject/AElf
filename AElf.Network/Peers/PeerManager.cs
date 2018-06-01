using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Network.Config;
using AElf.Network.Data;
using AElf.Network.Peers.Exceptions;
using Google.Protobuf;
using NLog;

namespace AElf.Network.Peers
{
    public class PeerManager : IPeerManager
    {
        public event EventHandler MessageReceived;
        
        private readonly IAElfNetworkConfig _networkConfig;
        private readonly IAElfServer _server;
        private readonly IPeerDatabase _peerDatabase;
        private readonly ILogger _logger;
        
        private readonly List<IPeer> _peers = new List<IPeer>();
        
        private readonly NodeData _nodeData;

        public PeerManager(IAElfServer server, IPeerDatabase peerDatabase, IAElfNetworkConfig config, ILogger logger)
        {
            _networkConfig = config;
            _logger = logger;
            _server = server;
            _peerDatabase = peerDatabase;

            _nodeData = new NodeData()
            {
                IpAddress = config.Host,
                Port = config.Port
            };
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
            Task.Run(Setup);
            
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
                foreach (var peerString in _networkConfig.Peers)
                {
                    // Parse the IP and port
                    string[] split = peerString.Split(':');

                    if (split.Length != 2)
                        continue;
                    
                    ushort port = ushort.Parse(split[1]);
                    
                    NodeData peer = new NodeData();
                    peer.IpAddress = split[0];
                    peer.Port = port;
                    
                    await CreatePeer(peer);
                }
            }

            var dbNodeData = _peerDatabase.ReadPeers();

            foreach (var p in dbNodeData)
            {
                await CreatePeer(p);
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
            try
            {
                PeerListData peerList = PeerListData.Parser.ParseFrom(messagePayload);

                foreach (var peer in peerList.NodeData)
                {
                    NodeData p = new NodeData();
                    p.IpAddress = peer.IpAddress;
                    p.Port = peer.Port;
                    IPeer newPeer = await CreatePeer(p);
                    _logger.Trace("Peer received : " + newPeer);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Invalid peer(s) - Could not receive peer(s) from the network", null);
            }
        }

        /// <summary>
        /// Adds a peer to the manager and hooks up the callback for
        /// receiving messages from it. It also starts the peers
        /// listening process.
        /// </summary>
        /// <param name="peer">the peer to add</param>
        public void AddPeer(IPeer peer)
        {
            // Temporary solution to checking if peer is already in _peers
            bool exists = false;
            foreach (var p in _peers)
            {
                exists = p.IpAddress == peer.IpAddress && p.Port == peer.Port;
            }
            
            if (exists) 
                return;
            
            _peers.Add(peer);
            
            peer.MessageReceived += ProcessPeerMessage;
            peer.PeerDisconnected += ProcessClientDisconnection;

            _logger.Trace("Peer added : " + peer);

            Task.Run(peer.StartListeningAsync);
        }
        
        /// <summary>
        /// Creates a Peer.
        /// </summary>
        /// <param name="nodeData"></param>
        /// <returns></returns>
        private async Task<IPeer> CreatePeer(NodeData nodeData)
        {
            IPeer peer = new Peer(_nodeData, nodeData);
            try
            {
                bool success = await peer.DoConnectAsync();
                    
                // If we successfully connected to the other peer
                // add it to be managed
                if (success)
                {
                    AddPeer(peer);
                    return peer;
                }
            }
            catch (ResponseTimeOutException rex)
            {
                _logger.Error(rex, rex?.Message + " - "  + peer);
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
            _logger.Trace("Peer removed : " + peer);
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

                    foreach (var peer in _peers)
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
                AElfPacketData packetData = new AElfPacketData
                {
                    Id = messageId,
                    MsgType = (int)messageType,
                    Length = payload.Length,
                    Payload = ByteString.CopyFrom(payload)
                };

                byte[] data = packetData.ToByteArray();

                foreach (var peer in _peers)
                {
                    await peer.SendAsync(data);
                }
                
                return true;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error while sending a message to the peers");
                return false;
            }
        }
    }
}