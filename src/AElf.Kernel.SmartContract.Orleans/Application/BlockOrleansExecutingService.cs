using AElf.Kernel.Blockchain;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Orleans.Application;

public class BlockOrleansExecutingService : IBlockExecutingService
{
    private readonly IOrleansTransactionExecutingService _orleansTransactionExecutingService;

    public BlockOrleansExecutingService(IOrleansTransactionExecutingService orleansTransactionExecutingService)
    {
        _orleansTransactionExecutingService = orleansTransactionExecutingService;
    }

    public Task<BlockExecutedSet> ExecuteBlockAsync(BlockHeader blockHeader,
        List<Transaction> nonCancellableTransactions)
    {
        throw new NotImplementedException();
    }

    public Task<BlockExecutedSet> ExecuteBlockAsync(BlockHeader blockHeader,
        IEnumerable<Transaction> nonCancellableTransactions,
        IEnumerable<Transaction> cancellableTransactions, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}