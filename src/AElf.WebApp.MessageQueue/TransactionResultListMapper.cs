using System;
using System.Collections.Generic;
using AElf.Kernel.Blockchain;
using AElf.Types;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AElf.WebApp.MessageQueue;

public class TransactionResultListMapper : IObjectMapper<BlockExecutedSet, TransactionResultListEto>,
    ITransientDependency
{
    private readonly IAutoObjectMappingProvider _mapperProvider;

    public TransactionResultListMapper(IAutoObjectMappingProvider mapperProvider)
    {
        _mapperProvider = mapperProvider;
    }

    public TransactionResultListEto Map(BlockExecutedSet source)
    {
        var transactionResultList = new TransactionResultListEto
        {
            StartBlockNumber = source.Height,
            EndBlockNumber = source.Height,
            ChainId = source.Block.Header.ChainId
        };

        var transactionResults = new Dictionary<string, List<TransactionResultEto>>();
        var blockHeight = source.Height;
        var blockHash = source.Block.Header.GetHash().ToHex();
        var blockTime = source.Block.Header.Time.ToDateTime();
        var transactionResultMap = source.TransactionResultMap;
        foreach (var transactionResultKeyPair in transactionResultMap)
        {
            var txId = transactionResultKeyPair.Key.ToHex();
            if (!source.TransactionMap.TryGetValue(transactionResultKeyPair.Key, out var transaction))
            {
                continue;
            }

            var newTransactionResult =
                _mapperProvider.Map<TransactionResult, TransactionResultEto>(transactionResultKeyPair.Value);
            FillTransactionInformation(blockHeight, blockHash, blockTime, newTransactionResult, transaction);
            if (transactionResults.TryGetValue(txId, out var txList))
            {
                txList.Add(newTransactionResult);
            }
            else
            {
                transactionResults.Add(txId, new List<TransactionResultEto> { newTransactionResult });
            }
        }

        transactionResultList.TransactionResults = transactionResults;
        return transactionResultList;
    }

    public TransactionResultListEto Map(BlockExecutedSet source, TransactionResultListEto destination)
    {
        throw new System.NotImplementedException();
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