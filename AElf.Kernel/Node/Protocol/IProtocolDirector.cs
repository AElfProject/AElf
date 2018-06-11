using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Network.Data;

namespace AElf.Kernel.Node.Protocol
{
    public interface IProtocolDirector
    {
        void Start();

        Task BroadcastTransaction(ITransaction transaction);
        void SetCommandContext(MainChainNode node);

        List<NodeData> GetPeers(ushort? numPeers);
    }
}