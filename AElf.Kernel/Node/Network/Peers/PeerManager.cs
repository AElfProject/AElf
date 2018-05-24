using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Node.Network.Config;
using AElf.Kernel.Node.Network.Data;
using AElf.Network;
using Google.Protobuf;
using NLog;
using ServiceStack;

namespace AElf.Kernel.Node.Network.Peers
{
    public class PeerManager : IPeerManager
    {
        private readonly IAElfNetworkConfig _networkConfig;
        private readonly IAElfServer _server;
        private readonly IPeerDatabase _peerDatabase;
        private readonly ILogger _logger;
        private List<IPeer> _peers;
        private List<IPeer> _peerDBContents;
        
        private MainChainNode _node;

        public PeerManager(IAElfServer server, IPeerDatabase peerDatabase, IAElfNetworkConfig config, ILogger logger)
        {
            _networkConfig = config;
            _logger = logger;
            _server = server;
            _peerDatabase = peerDatabase;

            _server.ClientConnected += HandleConnection;
        }
        
        /// <summary>
        /// Temporary solution, this is used for injecting a
        /// reference to the node.
        /// todo : remove dependency on the node
        /// </summary>
        /// <param name="node"></param>
        public void SetCommandContext(MainChainNode node)
        {
            _node = node;
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
            _peerDBContents = _peerDatabase.ReadPeers();
            Task.Run(() => _server.Start());
            Task.Run(Setup);
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
                    string[] splitted = peerString.Split(':');

                    if (splitted.Length != 2)
                        continue;
                    
                    ushort port = ushort.Parse(splitted[1]);
                    IPeer p = new Peer(splitted[0], port);
                    
                    bool success = await p.DoConnect();

                    // If we succesfully connected to the other peer 
                    // add it to be managed.
                    if (success)
                    {
                        AddPeer(p);
                    }
                }
            }

            if (_peerDBContents.Count > 0)
            {
                foreach (IPeer peer in _peerDBContents)
                {
                    bool success = await peer.DoConnect();
                    
                    // If we successfully connected to the other peer
                    // add it to be managed
                    if (success)
                    {
                        AddPeer(peer);
                    }
                }
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
            _peers.Add(peer);
            peer.MessageReceived += ProcessPeerMessage;
            
            _logger.Trace("Peer added : " + peer.IpAddress + ":" + peer.Port);
            
            Task.Run(peer.StartListeningAsync);
        }

        /// <summary>
        /// Callback that is executed when a peer receives a message.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ProcessPeerMessage(object sender, EventArgs e)
        {
            if (sender != null && e is MessageReceivedArgs args && args.Message != null)
            {
                if (args.Message.MsgType == (int)MessageTypes.BroadcastTx)
                {
                    await _node.ReceiveTransaction(args.Message.Payload);
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
        /// <returns></returns>
        public async Task BroadcastMessage(MessageTypes messageType, byte[] payload)
        {
            if (_peers == null || !_peers.Any())
                return;

            try
            {
                AElfPacketData packetData = new AElfPacketData
                {
                    MsgType = (int)messageType,
                    Length = payload.Length,
                    Payload = ByteString.CopyFrom(payload)
                };

                byte[] data = packetData.ToByteArray();

                foreach (var peer in _peers)
                {
                    await peer.Send(data);
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error while sending a message to the peers");
            }
        }
    }
}