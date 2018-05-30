using System.Collections.Generic;
using AElf.Kernel.Node.Network.Data;

namespace AElf.Kernel.Node.Network.Peers
{
    public interface IPeerDatabase
    {
        List<NodeData> ReadPeers();
        void WritePeers(List<NodeData> peerList);
    }
}