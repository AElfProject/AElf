using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Parallel.Domain
{
    public interface ICodeRemarksService
    {
        Task MarkUnparallelizableAsync(IChainContext chainContext, Address contractAddress);

        Task MarkParallelizableAsync(IChainContext chainContext, Address contractAddress);
    }
}