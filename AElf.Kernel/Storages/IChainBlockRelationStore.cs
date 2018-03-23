using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IChainBlockRelationStore
    {
        Task Insert(IChain chain, IBlock block,long height);
    }
}