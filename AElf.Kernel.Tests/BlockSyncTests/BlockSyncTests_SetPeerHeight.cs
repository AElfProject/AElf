using System.Threading.Tasks;
using AElf.Kernel.Node.Protocol;
using AElf.Network.Data;
using AElf.Network.Peers;
using Xunit;

namespace AElf.Kernel.Tests.BlockSyncTests
{
    public class BlockSyncTests_SetPeerHeight
    {
        #region Helpers and constants

        private NodeData _distant01 = new NodeData {IpAddress = "192.168.0.1"};
        private NodeData _distant02 = new NodeData {IpAddress = "192.168.0.1"};
        
        #endregion
        
        [Fact]
        public void SetPeerHeight_LowerHightThanCurrent_False()
        {
            BlockSynchronizer s = new BlockSynchronizer(null, null);
            
            s.SetNodeHeight(10);
            bool res = s.SetPeerHeight(new Peer(null, null), 5);
            
            Assert.False(res);
        }
        
        [Fact]
        public void SetPeerHeight_SamePeerHigher_True()
        {
            BlockSynchronizer s = new BlockSynchronizer(null, null);
            
            s.SetNodeHeight(2);

            Peer peer = new Peer(_distant01, null);
            
            bool res = s.SetPeerHeight(peer, 10);
            Assert.True(res);
            
            bool res2 = s.SetPeerHeight(peer, 12);
            Assert.True(res2);
        }
        
        [Fact]
        public void SetPeerHeight_SamePeerLower_True()
        {
            BlockSynchronizer s = new BlockSynchronizer(null, null);
            
            s.SetNodeHeight(2);
            
            Peer peer = new Peer(_distant01, null);
            
            bool res = s.SetPeerHeight(peer, 10);
            Assert.True(res);
            
            bool res2 = s.SetPeerHeight(peer, 5);
            Assert.False(res2);
        }
    }
}