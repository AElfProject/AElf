using System.Linq;
using AElf.Kernel.Blockchain.Domain;

namespace AElf.Kernel.Blockchain.Application;

public interface ITransactionResultQueryService
{
    Task<TransactionResult> GetTransactionResultAsync(Hash transactionId);
    Task<TransactionResult> GetTransactionResultAsync(Hash transactionId, Hash blockHash);
    Task<TransactionResult> GetFailedTransactionResultAsync(Hash transactionId);
}

public interface ITransactionResultService : ITransactionResultQueryService
{
    Task AddTransactionResultsAsync(IList<TransactionResult> transactionResult, BlockHeader blockHeader);
    Task ProcessTransactionResultAfterExecutionAsync(BlockHeader blockHeader, List<Hash> transactionIds);
    Task AddFailedTransactionResultsAsync(TransactionResult transactionResult);
}

public class TransactionResultService : ITransactionResultService, ITransientDependency
{
    private static readonly IEnumerable<TransactionResultStatus> FailStatus = new List<TransactionResultStatus>
    {
        TransactionResultStatus.Failed, TransactionResultStatus.NodeValidationFailed, TransactionResultStatus.Conflict
    };

    private readonly ITransactionBlockIndexService _transactionBlockIndexService;
    private readonly ITransactionResultManager _transactionResultManager;

    public TransactionResultService(ITransactionResultManager transactionResultManager,
        ITransactionBlockIndexService transactionBlockIndexService)
    {
        _transactionResultManager = transactionResultManager;
        _transactionBlockIndexService = transactionBlockIndexService;
    }

    public async Task AddFailedTransactionResultsAsync(TransactionResult transactionResult)
    {
        if (!FailStatus.Contains(transactionResult.Status)) return;
        await _transactionResultManager.AddFailedTransactionResultAsync(transactionResult);
    }
    
    public async Task AddFailedTransactionResultsAsync(IList<TransactionResult> transactionResults)
    {
        var failTransactionResult = transactionResults
            .Where(r => FailStatus.Contains(r.Status)).ToList();
        if (failTransactionResult.IsNullOrEmpty()) return;
        
        await _transactionResultManager.AddFailedTransactionResultsAsync(failTransactionResult);
    }

    public async Task<TransactionResult> GetFailedTransactionResultAsync(Hash transactionId)
    {
        var transactionResult = await _transactionResultManager.GetFailedTransactionResultAsync(transactionId);
        transactionResult.TransactionId = transactionId;
        return transactionResult;
    }

    public async Task AddTransactionResultsAsync(IList<TransactionResult> transactionResults,
        BlockHeader blockHeader)
    {
        await _transactionResultManager.AddTransactionResultsAsync(transactionResults,
            blockHeader.GetDisambiguatingHash());
    }

    public async Task<TransactionResult> GetTransactionResultAsync(Hash transactionId)
    {
        var transactionBlockIndex =
            await _transactionBlockIndexService.GetTransactionBlockIndexAsync(transactionId);

        if (transactionBlockIndex != null)
            return await _transactionResultManager.GetTransactionResultAsync(transactionId,
                transactionBlockIndex.BlockHash);

        return null;
    }

    public async Task<TransactionResult> GetTransactionResultAsync(Hash transactionId, Hash blockHash)
    {
        var txResult = await _transactionResultManager.GetTransactionResultAsync(transactionId, blockHash);
        return txResult;
    }

    public async Task ProcessTransactionResultAfterExecutionAsync(BlockHeader blockHeader,
        List<Hash> transactionIds)
    {
        var blockIndex = new BlockIndex
        {
            BlockHash = blockHeader.GetHash(),
            BlockHeight = blockHeader.Height
        };

        if (transactionIds.Count == 0)
            // This will only happen during test environment
            return;

        await _transactionBlockIndexService.AddBlockIndexAsync(transactionIds, blockIndex);
    }
}