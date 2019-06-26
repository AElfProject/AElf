using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Domain;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public interface IBlockchainExecutingService
    {
        Task<List<ChainBlockLink>> ExecuteBlocksAttachedToLongestChain(Chain chain, BlockAttachOperationStatus status);
    }
}