using System.Collections.Generic;
using AElf.Kernel.Node.Protocol;
using AElf.Network.Data;
using AElf.Network.Peers;
using Xunit;


namespace AElf.Kernel.Tests.BlockSyncTests
{
    public class BlockSyncTests_PeerHeightManagement
    {
        #region Helpers and constants

        private NodeData _distant01 = new NodeData {IpAddress = "192.168.0.1"};
        private NodeData _distant02 = new NodeData {IpAddress = "192.168.0.2"};
        private NodeData _distant03 = new NodeData {IpAddress = "192.168.0.1"};
        
        #endregion
        
        /*[Fact(Skip = "NullReferenceException")]
        public void SetPeerHeight_LowerHightThanCurrent_False()
        {
            BlockSynchronizer s = new BlockSynchronizer(null, null);
            
            s.SetNodeHeight(10);
            bool res = s.SetPeerHeight(new Peer(null, null), 5);
            
            Assert.False(res);
        }*/
        
        [Fact(Skip = "todo")]
        public void SetPeerHeight_SamePeerHigher_True()
        {
            /*
            BlockSynchronizer s = new BlockSynchronizer(null, null);
            
            s.SetNodeHeight(2);

            Peer peer = new Peer(_distant01, null);
            
            bool res = s.SetPeerHeight(peer, 10);
            Assert.True(res);
            
            bool res2 = s.SetPeerHeight(peer, 12);
            Assert.True(res2);
            */
        }
        
        [Fact(Skip = "todo")]
        public void SetPeerHeight_SamePeerLower_True()
        {
            /*
            BlockSynchronizer s = new BlockSynchronizer(null, null);
            
            s.SetNodeHeight(2);
            
            Peer peer = new Peer(_distant01, null);
            
            bool res = s.SetPeerHeight(peer, 10);
            Assert.True(res);
            
            bool res2 = s.SetPeerHeight(peer, 5);
            Assert.False(res2);
            */
        }
        
        [Fact(Skip = "todo")]
        public void RemovePeerOldPeers_RemovesPeersThatAreBehind()
        {
            /*
            BlockSynchronizer s = new BlockSynchronizer(null, null);
            s.SetNodeHeight(2);
            
            IPeer peer = new Peer(_distant01, null);
            IPeer peer2 = new Peer(_distant02, null);
            IPeer peer3 = new Peer(_distant03, null);
            
            s.SetPeerHeight(peer, 5);
            s.SetPeerHeight(peer2, 10);
            s.SetPeerHeight(peer3, 20);

            s.SetNodeHeight(20);
            
            List<IPeer> removed = s.RemoveLowerHeightPeers();
            
            Assert.NotNull(removed);
            Assert.Equal(2, removed.Count);
            Assert.True(removed.Contains(peer));
            Assert.True(removed.Contains(peer2));
            */
        }
    }
}