using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Node.Network.Data;

namespace AElf.Kernel.Node.Network.Peers
{
    public class PeerManager : IPeerManager
    {
        private IAElfServer _server;
        private MainChainNode _node;
        
        // todo : this is maybe node db ?
        //private IPeerDatabase _peerDatabase;
        
        private readonly List<Peer> _peers;

        public PeerManager(IAElfServer server, IPeerDatabase peerDatabase)
        {
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
    }
}