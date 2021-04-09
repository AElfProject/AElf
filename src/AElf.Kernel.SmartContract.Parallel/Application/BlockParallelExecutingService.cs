using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.SmartContractExecution.Infrastructure;

namespace AElf.Kernel.SmartContract.Parallel.Application
{
    public class BlockParallelExecutingService : BlockExecutingService
    {
        public BlockParallelExecutingService(ITransactionExecutingService transactionExecutingService,
            IBlockchainStateService blockchainStateService, ITransactionResultService transactionResultService,
            ISystemTransactionExtraDataProvider systemTransactionExtraDataProvider,
            IExecutedTransactionResultCacheProvider executedTransactionResultCacheProvider) : base(
            transactionExecutingService, blockchainStateService, transactionResultService,
            systemTransactionExtraDataProvider, executedTransactionResultCacheProvider)
        {
        }

        protected override async Task CleanUpReturnSetCollectionAsync(BlockHeader blockHeader, ExecutionReturnSetCollection executionReturnSetCollection)
        {
            await base.CleanUpReturnSetCollectionAsync(blockHeader, executionReturnSetCollection);
        }
    }
}