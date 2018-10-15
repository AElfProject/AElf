using System.Collections.Generic;
using AElf.Kernel;

namespace AElf.Network.Peers
{
    public partial class Peer
    {   
        private int _peerAcceptHeight = 0;
        private int _localAcceptHeight = 0;
        private bool _isSyncPeer = false;
        
        public int KnownHeight
        {
            get { return _peerAcceptHeight; }
        }
        
        private void OnBlockReceived(Block block)
        {
            
        }

        public void Sync()
        {
            if (_localAcceptHeight < _peerAcceptHeight)
            {
                //sync 
            }
        }

        public void OnNewBlockAccepted()
        {
            _localAcceptHeight++;
        }

        public void OnNewBlockMined()
        {
            
        }
    }
}