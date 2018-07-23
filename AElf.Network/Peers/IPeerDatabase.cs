using System.Collections.Generic;
using NodeData = AElf.Network.Data.Protobuf.NodeData;

namespace AElf.Network.Peers
{
    public interface IPeerDatabase
    {
        List<NodeData> ReadPeers();
        void WritePeers(List<NodeData> peerList);
    }
}