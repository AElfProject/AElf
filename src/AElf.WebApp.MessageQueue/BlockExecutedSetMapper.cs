using AElf.Kernel;
using AElf.Kernel.Blockchain;
using AElf.Types;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AElf.WebApp.MessageQueue;

public class BlockExecutedSetMapper : IObjectMapper<BlockExecutedSet, BlockMessageEto>, ITransientDependency
{
    private readonly IAutoObjectMappingProvider _mapperProvider;

    public BlockExecutedSetMapper(IAutoObjectMappingProvider mapperProvider)
    {
        _mapperProvider = mapperProvider;
    }

    public BlockMessageEto Map(BlockExecutedSet source)
    {
        var blockMessageEto = _mapperProvider.Map<Block, BlockMessageEto>(source.Block);
        var transactionResultMap = source.TransactionResultMap;
        foreach (var transactionResultKeyPair in transactionResultMap)
        {
            if(!source.TransactionMap.TryGetValue(transactionResultKeyPair.Key, out var transaction))
            {
                continue; // todo add log
            }
            
            var transactionMessageEto = _mapperProvider.Map<TransactionResult, TransactionMessageEto>(transactionResultKeyPair.Value);
            FillTransactionInformation(transactionMessageEto, transaction);
            blockMessageEto.TransactionMessageList.Add(transactionMessageEto);
        }

        return blockMessageEto;
    }

    public BlockMessageEto Map(BlockExecutedSet source, BlockMessageEto destination)
    {
        throw new System.NotImplementedException();
    }

    private void FillTransactionInformation(TransactionMessageEto transactionMessage, Transaction transaction)
    {
        transactionMessage.MethodName = transaction.MethodName;
        transactionMessage.FromAddress = transaction.From.ToBase58();
        transactionMessage.ToAddress = transaction.To.ToBase58();
    }
}