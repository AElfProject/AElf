using System.Collections.Generic;

namespace AElf.Kernel.Node.Network.Peers
{
    public interface IPeerDatabase
    {
        List<IPeer> ReadPeers();
        void WritePeers(List<IPeer> peerList);
    }
}