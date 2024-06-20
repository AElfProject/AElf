using AElf.Kernel.Blockchain.Domain;

namespace AElf.Kernel.Blockchain.Application;

public interface ITransactionResultQueryService
{
    Task<TransactionResult> GetTransactionResultAsync(Hash transactionId);
    Task<TransactionResult> GetTransactionResultAsync(Hash transactionId, Hash blockHash);
}

public interface ITransactionResultService : ITransactionResultQueryService
{
    Task AddTransactionResultsAsync(IList<TransactionResult> transactionResult, BlockHeader blockHeader);

    Task ProcessTransactionResultAfterExecutionAsync(BlockHeader blockHeader, List<Hash> transactionIds);
    
    Task<List<TransactionResult>> GetTransactionResultsAsync(IList<Hash> transactionIds, Hash blockHash);
}

public class TransactionResultService : ITransactionResultService, ITransientDependency
{
    private readonly ITransactionBlockIndexService _transactionBlockIndexService;
    private readonly ITransactionResultManager _transactionResultManager;

    public TransactionResultService(ITransactionResultManager transactionResultManager,
        ITransactionBlockIndexService transactionBlockIndexService)
    {
        _transactionResultManager = transactionResultManager;
        _transactionBlockIndexService = transactionBlockIndexService;
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

    public async Task<List<TransactionResult>> GetTransactionResultsAsync(IList<Hash> transactionIds, Hash blockHash)
    {
        return await _transactionResultManager.GetTransactionResultsAsync(transactionIds, blockHash);
    }
}