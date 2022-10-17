using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Parallel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContract.Orleans.Application;

public class OrleansTransactionExecutingService : IOrleansTransactionExecutingService
{
    private readonly IOrleansTransactionExecutingClientService _orleansTransactionExecutingClientService;
    private readonly ITransactionGrouper _grouper;

    public ILogger<OrleansTransactionExecutingService> Logger { get; set; }
    public ILocalEventBus EventBus { get; set; }

    public OrleansTransactionExecutingService(
        IOrleansTransactionExecutingClientService orleansTransactionExecutingClientService, ITransactionGrouper grouper)
    {
        _orleansTransactionExecutingClientService = orleansTransactionExecutingClientService;
        _grouper = grouper;
        EventBus = NullLocalEventBus.Instance;
        Logger = NullLogger<OrleansTransactionExecutingService>.Instance;
    }

    public async Task<List<ExecutionReturnSet>> ExecuteAsync(TransactionExecutingDto transactionExecutingDto,
        CancellationToken cancellationToken)
    {
        var transactions = transactionExecutingDto.Transactions.ToList();
        var blockHeader = transactionExecutingDto.BlockHeader;
        var groupedTransactions = await _grouper.GroupAsync(new ChainContext
        {
            BlockHash = blockHeader.PreviousBlockHash,
            BlockHeight = blockHeader.Height - 1
        }, transactions);

        var returnSets = new List<ExecutionReturnSet>();
        
        // TODO: Further impl.
        
        return returnSets;
    }

}