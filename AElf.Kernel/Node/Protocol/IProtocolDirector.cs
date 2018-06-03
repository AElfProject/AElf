using System.Threading.Tasks;

namespace AElf.Kernel.Node.Protocol
{
    public interface IProtocolDirector
    {
        void Start();

        Task BroadcastTransaction(ITransaction transaction);
        void SetCommandContext(MainChainNode node);
    }
}