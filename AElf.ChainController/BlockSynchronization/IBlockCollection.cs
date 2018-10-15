using System.Threading.Tasks;
using AElf.Kernel;

// ReSharper disable once CheckNamespace
namespace AElf.ChainController
{
    public interface IBlockCollection
    {
        Task AddBlock(IBlock block);
        Task Tell(ulong currentHeight);
    }
}