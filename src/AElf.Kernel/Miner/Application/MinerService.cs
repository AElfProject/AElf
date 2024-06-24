using System.Collections.Generic;
using AElf.Kernel.Blockchain;
using AElf.Kernel.TransactionPool.Application;
using AElf.Kernel.Txn.Application;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Miner.Application;

public class MinerService : IMinerService
{
    public ILogger<MinerService> Logger { get; set; }
    private readonly ITransactionPoolService _transactionPoolService;
    private readonly IMiningService _miningService;
    private readonly IBlockTransactionLimitProvider _blockTransactionLimitProvider;
    private readonly ITransactionPackingOptionProvider _transactionPackingOptionProvider;

    public MinerService(IMiningService miningService,
        IBlockTransactionLimitProvider blockTransactionLimitProvider,
        ITransactionPackingOptionProvider transactionPackingOptionProvider,
        ITransactionPoolService transactionPoolService)
    {
        _miningService = miningService;
        _blockTransactionLimitProvider = blockTransactionLimitProvider;
        _transactionPackingOptionProvider = transactionPackingOptionProvider;
        _transactionPoolService = transactionPoolService;

        Logger = NullLogger<MinerService>.Instance;
    }

    /// <inheritdoc />
    /// <summary>
    /// Mine process.
    /// </summary>
    /// <returns></returns>
    public async Task<BlockExecutedSet> MineAsync(Hash previousBlockHash, long previousBlockHeight,
        Timestamp blockTime,
        Duration blockExecutionTime)
    {
        Logger.LogTrace("Begin MinerService.MineAsync");
        var txList = new List<Transaction>(150000);
            
        var chainContext = new ChainContext
        {
            BlockHash = previousBlockHash,
            BlockHeight = previousBlockHeight
        };

        var limit = await _blockTransactionLimitProvider.GetLimitAsync(chainContext);
            
        if (_transactionPackingOptionProvider.IsTransactionPackable(chainContext))
        {
            var executableTransactionSet = await _transactionPoolService.GetExecutableTransactionSetAsync(txList,
                previousBlockHash, limit);

            // txList.AddRange(executableTransactionSet.Transactions);
        }
            

        Logger.LogInformation(
            $"Start mining with previous hash: {previousBlockHash}, previous height: {previousBlockHeight}.");
            
        Logger.LogTrace("Begin mine block.");
        var blockExecuteSet = await _miningService.MineAsync(
            new RequestMiningDto
            {
                PreviousBlockHash = previousBlockHash,
                PreviousBlockHeight = previousBlockHeight,
                BlockExecutionTime = blockExecutionTime,
                TransactionCountLimit = limit
            }, txList, blockTime);
        Logger.LogTrace("End MinerService.MineAsync");

        return blockExecuteSet;
    }
}