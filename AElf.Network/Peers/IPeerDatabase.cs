using System.Collections.Generic;
using AElf.Network.Data;

namespace AElf.Network.Peers
{
    public interface IPeerDatabase
    {
        List<NodeData> ReadPeers();
        void WritePeers(List<NodeData> peerList);
    }
}