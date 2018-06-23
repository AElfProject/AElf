using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Types;
using AElf.Network.Data;

namespace AElf.Kernel.Node.Protocol
{
    public interface IProtocolDirector
    {
        void Start();

        Task BroadcastBlock(Block block);
        Task BroadcastTransaction(ITransaction transaction);
        
        void SetCommandContext(MainChainNode node, bool doSync = false);

        List<NodeData> GetPeers(ushort? numPeers);
        void AddTransaction(Transaction tx);
    }
}