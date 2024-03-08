using System.Diagnostics;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.Logging;
using Orleans;

namespace AElf.Kernel.SmartContract.Orleans;

public class PlainTransactionExecutingGrain : Grain, IPlainTransactionExecutingGrain
{
    private readonly ILogger<PlainTransactionExecutingGrain> _logger;
    private readonly IPlainTransactionExecutingService _plainTransactionExecutingService;


    public PlainTransactionExecutingGrain(ILogger<PlainTransactionExecutingGrain> logger,
        IPlainTransactionExecutingService plainTransactionExecutingService)
    {
        _logger = logger;
        _plainTransactionExecutingService = plainTransactionExecutingService;
    }

    public async Task<List<ExecutionReturnSet>> ExecuteAsync(TransactionExecutingDto transactionExecutingDto,
        CancellationToken cancellationToken)
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            var executionReturnSet =
                await _plainTransactionExecutingService.ExecuteAsync(transactionExecutingDto, cancellationToken);
            _logger.LogDebug("groupType:{groupType}, height: {height},txCount:{count}, time: {time}ms",
                transactionExecutingDto.GroupType, transactionExecutingDto.BlockHeader.Height,
                transactionExecutingDto.Transactions.Count(),
                stopwatch.ElapsedMilliseconds);
            return executionReturnSet;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed while executing txs in block");
            throw;
        }
    }
}