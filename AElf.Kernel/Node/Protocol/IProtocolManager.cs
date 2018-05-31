using System.Threading.Tasks;

namespace AElf.Kernel.Node.Protocol
{
    public interface IProtocolManager
    {
        void Start();

        Task BroadcastTransaction(byte[] transaction);
        void SetCommandContext(MainChainNode node);
    }
}