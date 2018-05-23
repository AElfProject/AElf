using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Node.Network.Data;
using AElf.Network;
using Google.Protobuf;
using NLog;

namespace AElf.Kernel.Node.Network.Peers
{
    public class PeerManager : IPeerManager
    {
        private readonly IAElfServer _server;
        private readonly ILogger _logger;
        private readonly List<Peer> _peers;
        
        private MainChainNode _node;

        public PeerManager(IAElfServer server, IPeerDatabase peerDatabase, ILogger logger)
        {
            _logger = logger;
            _server = server;
            _peers = new List<Peer>();

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

        public void Start()
        {
            _server.Start();
        }

        public void AddPeer(Peer peer)
        {
            _peers.Add(peer);
            peer.MessageReceived += ProcessPeerMessage;
            
            Task.Run(peer.StartListeningAsync);
        }

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
        /// This message broadcasts data to all of its peers. The creates and
        /// send a <see cref="AElfPacketData"/> object with the provided pay-
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