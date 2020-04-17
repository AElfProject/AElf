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

        protected override async Task<Block> FillBlockAfterExecutionAsync(BlockHeader blockHeader,
            List<Transaction> transactions, ReturnSetCollection returnSetCollection, BlockStateSet blockStateSet)
        {
            var block = await base.FillBlockAfterExecutionAsync(blockHeader, transactions, returnSetCollection, blockStateSet);
            if (returnSetCollection.Conflict.Count > 0)
            {
                await EventBus.PublishAsync(new ConflictingTransactionsFoundInParallelGroupsEvent(
                    block.Header, returnSetCollection.Executed.Concat(returnSetCollection.Unexecutable).ToList(),
                    returnSetCollection.Conflict
                ));
            }

            return block;
        }
    }
}