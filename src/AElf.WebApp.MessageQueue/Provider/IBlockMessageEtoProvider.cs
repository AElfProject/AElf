using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Types;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace AElf.WebApp.MessageQueue.Provider;

public interface IBlockMessageEtoProvider
{
    Task<BlockMessageEto> GetBlockMessageEtoByHeightAsync(long height, CancellationToken cts);
    BlockMessageEto GetBlockMessageEto(BlockExecutedSet blockExecutedSet);
}

public class BlockMessageEtoProvider : IBlockMessageEtoProvider, ISingletonDependency
{
    private readonly IBlockchainService _blockchainService;
    private readonly ITransactionResultQueryService _transactionResultQueryService;
    private readonly ITransactionManager _transactionManager;
    private readonly IObjectMapper _objectMapper;

    public BlockMessageEtoProvider(IBlockchainService blockchainService,
        ITransactionResultQueryService transactionResultQueryService, ITransactionManager transactionManager,
        IObjectMapper objectMapper)
    {
        _blockchainService = blockchainService;
        _transactionResultQueryService = transactionResultQueryService;
        _transactionManager = transactionManager;
        _objectMapper = objectMapper;
    }

    public async Task<BlockMessageEto> GetBlockMessageEtoByHeightAsync(long height, CancellationToken cts)
    {
        var block = await GetBlockByHeightAsync(height);
        if (block == null)
        {
            // todo add log
            return null;
        }
        
        var blockMessageEto = _objectMapper.Map<Block, BlockMessageEto>(block);
        var blockHash = block.Header.GetHash();

        foreach (var txId in block.TransactionIds)
        {
            if (cts.IsCancellationRequested)
            {
                return null;
            }

            var transactionResult = await _transactionResultQueryService.GetTransactionResultAsync(txId, blockHash);
            if (transactionResult == null)
            {
                continue; // todo add log
            }

            var transaction = await _transactionManager.GetTransactionAsync(txId);
            if (transaction == null)
            {
                continue; // todo add log
            }

            var transactionMessageEto = _objectMapper.Map<TransactionResult, TransactionMessageEto>(transactionResult);
            FillTransactionInformation(transactionMessageEto, transaction);
            blockMessageEto.TransactionMessageList.Add(transactionMessageEto);
        }

        return blockMessageEto;
    }

    public BlockMessageEto GetBlockMessageEto(BlockExecutedSet blockExecutedSet)
    {
        return _objectMapper.Map<BlockExecutedSet, BlockMessageEto>(blockExecutedSet);
    }

    private async Task<Block> GetBlockByHeightAsync(long height)
    {
        var chain = await _blockchainService.GetChainAsync();
        var hash = await _blockchainService.GetBlockHashByHeightAsync(chain, height, chain.LongestChainHash);
        var blocks = await _blockchainService.GetBlocksInLongestChainBranchAsync(hash, 1);
        return blocks.Any() ? blocks.First() : null;
    }

    private void FillTransactionInformation(TransactionMessageEto transactionMessage, Transaction transaction)
    {
        transactionMessage.MethodName = transaction.MethodName;
        transactionMessage.FromAddress = transaction.From.ToBase58();
        transactionMessage.ToAddress = transaction.To.ToBase58();
    }
}