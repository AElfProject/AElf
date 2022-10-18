using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.TransactionPool;
using AElf.Kernel.TransactionPool.Application;

namespace AElf.Kernel.Miner.Application;

public class TxPoolInterestedEventsHandler : ILocalEventHandler<TransactionsReceivedEvent>,
    ILocalEventHandler<BlockAcceptedEvent>,
    ILocalEventHandler<BestChainFoundEventData>,
    ILocalEventHandler<NewIrreversibleBlockFoundEvent>,
    ITransientDependency
{
    private readonly ITransactionPoolService _transactionPoolService;

    public TxPoolInterestedEventsHandler(ITransactionPoolService transactionPoolService)
    {
        _transactionPoolService = transactionPoolService;
    }

    public async Task HandleEventAsync(BestChainFoundEventData eventData)
    {
        await _transactionPoolService.UpdateTransactionPoolByBestChainAsync(eventData.BlockHash, eventData.BlockHeight);
    }

    public async Task HandleEventAsync(BlockAcceptedEvent eventData)
    {
        await _transactionPoolService.CleanByTransactionIdsAsync(eventData.Block.TransactionIds);
    }

    public async Task HandleEventAsync(NewIrreversibleBlockFoundEvent eventData)
    {
        await _transactionPoolService.UpdateTransactionPoolByLibAsync(eventData.BlockHeight);
    }

    public async Task HandleEventAsync(TransactionsReceivedEvent eventData)
    {
        await _transactionPoolService.AddTransactionsAsync(eventData.Transactions);
    }
}