using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Node.Network.Peers
{
    public class PeerManager : IPeerManager
    {
        private IAElfServer _server;
        
        // todo : this is maybe node db ?
        //private IPeerDatabase _peerDatabase;
        
        private readonly List<Peer> _peers;

        public PeerManager(IAElfServer server, IPeerDatabase peerDatabase)
        {
            _server = server;
            _peers = new List<Peer>();

            _server.ClientConnected += HandleConnection;
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
            Task.Run(peer.StartListeningAsync);
        }
    }
}