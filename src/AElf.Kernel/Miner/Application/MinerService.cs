using System.Collections.Generic;
using AElf.Kernel.Blockchain;
using AElf.Kernel.TransactionPool;
using AElf.Kernel.TransactionPool.Application;
using AElf.Kernel.Txn.Application;
using Google.Protobuf.WellKnownTypes;

namespace AElf.Kernel.Miner.Application;

public class MinerService : IMinerService
{
    private readonly IBlockTransactionLimitProvider _blockTransactionLimitProvider;
    private readonly IMiningService _miningService;
    private readonly ITransactionPackingOptionProvider _transactionPackingOptionProvider;
    private readonly ITransactionPoolService _transactionPoolService;

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

    public ILogger<MinerService> Logger { get; set; }

    /// <inheritdoc />
    /// <summary>
    ///     Mine process.
    /// </summary>
    /// <returns></returns>
    public async Task<BlockExecutedSet> MineAsync(Hash previousBlockHash, long previousBlockHeight,
        Timestamp blockTime,
        Duration blockExecutionTime)
    {
        var txList = new List<Transaction>(TransactionOptions.BlockTransactionLimit);
        var chainContext = new ChainContext
        {
            BlockHash = previousBlockHash,
            BlockHeight = previousBlockHeight
        };

        var limit = await _blockTransactionLimitProvider.GetLimitAsync(chainContext);
        
        if (_transactionPackingOptionProvider.IsTransactionPackable(chainContext))
        {
            var executableTransactionSet = await _transactionPoolService.GetExecutableTransactionSetAsync(
                previousBlockHash, limit);

            txList.AddRange(executableTransactionSet.Transactions);
        }

        // Logger.LogInformation(
            // "Start mining with previous hash: {PreviousBlockHash}, previous height: {PreviousBlockHeight}",
            // previousBlockHash.ToHex(), previousBlockHeight);
        return await _miningService.MineAsync(
            new RequestMiningDto
            {
                PreviousBlockHash = previousBlockHash,
                PreviousBlockHeight = previousBlockHeight,
                BlockExecutionTime = blockExecutionTime,
                TransactionCountLimit = limit
            }, txList, blockTime);
    }
}