using AElf.Contracts.Genesis;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Orleans;

namespace AElf.Kernel.SmartContract.Orleans;

public class PlainTransactionExecutingGrain : Grain, IPlainTransactionExecutingGrain
{
    private readonly ILogger<PlainTransactionExecutingGrain> _logger;
    private readonly PlainTransactionExecutingService _plainTransactionExecutingService;


    public PlainTransactionExecutingGrain(ILogger<PlainTransactionExecutingGrain> logger,
        PlainTransactionExecutingService plainTransactionExecutingService)
    {
        _logger = logger;
        _plainTransactionExecutingService = plainTransactionExecutingService;
    }

    public async Task<List<ExecutionReturnSet>> ExecuteAsync(TransactionExecutingDto transactionExecutingDto,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("PlainTransactionExecutingGrain.ExecuteAsync, groupType:{groupType}, height: {height},txCount:{count}",
                transactionExecutingDto.GroupType, transactionExecutingDto.BlockHeader.Height, transactionExecutingDto.Transactions.Count());

           return await _plainTransactionExecutingService.ExecuteAsync(transactionExecutingDto, cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed while executing txs in block");
            throw;
        }
    }
}