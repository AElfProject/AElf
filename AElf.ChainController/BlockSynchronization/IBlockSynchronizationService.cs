using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.ChainController
{
    public interface IBlockSynchronizationService
    {
        Task ReceiveBlock(IBlock block);
    }
}