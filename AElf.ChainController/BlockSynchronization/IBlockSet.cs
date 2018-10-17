using System.Threading.Tasks;
using AElf.Kernel;

// ReSharper disable once CheckNamespace
namespace AElf.ChainController
{
    public interface IBlockSet
    {
        Task AddBlock(IBlock block);
        Task Tell(ulong currentHeight);
    }
}