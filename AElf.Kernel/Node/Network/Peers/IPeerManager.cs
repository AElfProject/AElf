using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Node.Network.Data;

namespace AElf.Kernel.Node.Network.Peers
{
    public interface IPeerManager
    {
        void Start();
        void AddPeer(IPeer peer);
        List<IPeer> GetPeers(ushort numPeers);

        Task BroadcastMessage(MessageTypes messageType, byte[] payload);
        
        void SetCommandContext(MainChainNode node);
    }
}