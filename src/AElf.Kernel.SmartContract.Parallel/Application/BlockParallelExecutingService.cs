using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Parallel.Application
{
    public class BlockParallelExecutingService : BlockExecutingService
    {
        public BlockParallelExecutingService(ITransactionExecutingService transactionExecutingService,
            IBlockchainStateService blockchainStateService, ITransactionResultService transactionResultService) : base(
            transactionExecutingService, blockchainStateService, transactionResultService)
        {
        }

        protected override async Task CleanUpReturnSetCollectionAsync(BlockHeader blockHeader, ReturnSetCollection returnSetCollection)
        {
            await base.CleanUpReturnSetCollectionAsync(blockHeader, returnSetCollection);
            if (returnSetCollection.Conflict.Count > 0)
            {
                await EventBus.PublishAsync(new ConflictingTransactionsFoundInParallelGroupsEvent(
                    blockHeader, returnSetCollection.Executed.Concat(returnSetCollection.Unexecutable).ToList(),
                    returnSetCollection.Conflict
                ));
            }
        }
    }
}