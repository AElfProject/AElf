using System.Threading.Tasks;

namespace AElf.Kernel.Storages
{
    public interface IBlockStore
    {
        Task Insert(IBlock block);
    }
}