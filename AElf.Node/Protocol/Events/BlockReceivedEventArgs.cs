using System;
using AElf.Kernel;
using AElf.Network.Peers;

namespace AElf.Node.Protocol.Events
{
    public class BlockReceivedEventArgs : EventArgs
    {
        public Block Block { get; private set; }
        public IPeer Peer { get; private set; }

        public BlockReceivedEventArgs(Block block, IPeer peer)
        {
            Block = block;
            Peer = peer;
        }
    }
}