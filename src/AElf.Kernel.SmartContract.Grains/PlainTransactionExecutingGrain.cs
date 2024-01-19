using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.Logging;
namespace AElf.Kernel.SmartContract.Grains;

public class PlainTransactionExecutingGrain : Orleans.Grain, IPlainTransactionExecutingGrain
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
            _logger.LogDebug("groupType:{groupType}, height: {height},txCount:{count}",
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