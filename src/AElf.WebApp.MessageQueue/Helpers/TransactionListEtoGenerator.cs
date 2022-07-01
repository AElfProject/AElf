using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Volo.Abp.ObjectMapping;

namespace AElf.WebApp.MessageQueue.Helpers;

public class TransactionListEtoGenerator : IBlockMessageEtoGenerator
{
    private readonly IBlockchainService _blockchainService;
    private readonly ITransactionResultQueryService _transactionResultQueryService;
    private readonly ITransactionManager _transactionManager;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<TransactionListEtoGenerator> _logger;

    public TransactionListEtoGenerator(IBlockchainService blockchainService,
        ITransactionResultQueryService transactionResultQueryService, ITransactionManager transactionManager,
        IObjectMapper objectMapper, ILogger<TransactionListEtoGenerator> logger)
    {
        _blockchainService = blockchainService;
        _transactionResultQueryService = transactionResultQueryService;
        _transactionManager = transactionManager;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task<IBlockMessage> GetBlockMessageEtoByHeightAsync(long height, CancellationToken cts)
    {
        var block = await GetBlockByHeightAsync(height);
        if (block == null)
        {
            _logger.LogWarning($"Failed to find block information, height: {height + 1}");
            return null;
        }

        var blockHash = block.Header.GetHash();
        var blockHashStr = blockHash.ToHex();
        var blockHeight = block.Height;
        var blockTime = block.Header.Time.ToDateTime();
        var transactionResultList = new TransactionResultListEto
        {
            StartBlockNumber = blockHeight,
            EndBlockNumber = blockHeight,
            ChainId = block.Header.ChainId
        };
        var transactionResults = new Dictionary<string, List<TransactionResultEto>>();

        foreach (var txId in block.TransactionIds)
        {
            if (cts.IsCancellationRequested)
            {
                return null;
            }

            var transactionResult = await _transactionResultQueryService.GetTransactionResultAsync(txId, blockHash);
            if (transactionResult == null)
            {
                _logger.LogWarning(
                    $"Failed to find transactionResult, block hash: {blockHash},  transaction ID: {txId}");
                continue;
            }

            var transaction = await _transactionManager.GetTransactionAsync(txId);
            if (transaction == null)
            {
                _logger.LogWarning($"Failed to find transaction, block hash: {blockHash},  transaction ID: {txId}");
                continue;
            }

            var newTransactionResult = _objectMapper.Map<TransactionResult, TransactionResultEto>(transactionResult);
            FillTransactionInformation(blockHeight, blockHashStr, blockTime, newTransactionResult, transaction);
            if (transactionResults.TryGetValue(newTransactionResult.TransactionId, out var txList))
            {
                txList.Add(newTransactionResult);
            }
            else
            {
                transactionResults.Add(newTransactionResult.TransactionId,
                    new List<TransactionResultEto> { newTransactionResult });
            }
        }

        transactionResultList.TransactionResults = transactionResults;
        return transactionResultList;
    }

    public IBlockMessage GetBlockMessageEto(BlockExecutedSet blockExecutedSet)
    {
        return _objectMapper.Map<BlockExecutedSet, TransactionResultListEto>(blockExecutedSet);
    }

    private async Task<Block> GetBlockByHeightAsync(long height)
    {
        var chain = await _blockchainService.GetChainAsync();
        var hash = await _blockchainService.GetBlockHashByHeightAsync(chain, height, chain.LongestChainHash);
        var blocks = await _blockchainService.GetBlocksInLongestChainBranchAsync(hash, 1);
        return blocks.Any() ? blocks.First() : null;
    }

    private static void FillTransactionInformation(long blockHeight, string blockHash, DateTime blockTime,
        TransactionResultEto transactionMessage, Transaction transaction)
    {
        transactionMessage.BlockHash = blockHash;
        transactionMessage.BlockNumber = blockHeight;
        transactionMessage.BlockTime = blockTime;
        transactionMessage.MethodName = transaction.MethodName;
        transactionMessage.FromAddress = transaction.From.ToBase58();
        transactionMessage.ToAddress = transaction.To.ToBase58();
    }
}