using System;
using System.Linq;
using System.Runtime.Serialization;

namespace AElf.Kernel.Blockchain.Application;

public interface IBlockValidationProvider
{
    Task<bool> ValidateBeforeAttachAsync(IBlock block);
    Task<bool> ValidateBlockBeforeExecuteAsync(IBlock block);
    Task<bool> ValidateBlockAfterExecuteAsync(IBlock block);
}

[Serializable]
public class BlockValidationException : Exception
{
    //
    // For guidelines regarding the creation of new exception types, see
    //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
    // and
    //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
    //

    public BlockValidationException()
    {
    }

    public BlockValidationException(string message) : base(message)
    {
    }

    public BlockValidationException(string message, Exception inner) : base(message, inner)
    {
    }

    protected BlockValidationException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }
}

[Serializable]
public class ValidateNextTimeBlockValidationException : BlockValidationException
{
    //
    // For guidelines regarding the creation of new exception types, see
    //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
    // and
    //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
    //

    public ValidateNextTimeBlockValidationException()
    {
    }

    public ValidateNextTimeBlockValidationException(string message) : base(message)
    {
    }

    public ValidateNextTimeBlockValidationException(string message, Exception inner) : base(message, inner)
    {
    }

    public ValidateNextTimeBlockValidationException(Hash blockhash) : this(
        $"validate next time, block hash = {blockhash.ToHex()}")
    {
        BlockHash = blockhash;
    }

    protected ValidateNextTimeBlockValidationException(
        SerializationInfo info,
        StreamingContext context) : base(info, context)
    {
    }

    public Hash BlockHash { get; private set; }
}

public class BlockValidationProvider : IBlockValidationProvider
{
    private readonly IBlockchainService _blockchainService;
    private readonly ITransactionBlockIndexService _transactionBlockIndexService;

    public BlockValidationProvider(IBlockchainService blockchainService,
        ITransactionBlockIndexService transactionBlockIndexService)
    {
        _blockchainService = blockchainService;
        _transactionBlockIndexService = transactionBlockIndexService;
    }

    public ILogger<BlockValidationProvider> Logger { get; set; }

    public Task<bool> ValidateBeforeAttachAsync(IBlock block)
    {
        if (block?.Header == null || block.Body == null)
        {
            Logger.LogDebug("Block header or body is null");
            return Task.FromResult(false);
        }

        if (block.Body.TransactionsCount == 0)
        {
            Logger.LogDebug("Block transactions is empty");
            return Task.FromResult(false);
        }

        var hashSet = new HashSet<Hash>();
        if (block.Body.TransactionIds.Select(item => hashSet.Add(item)).Any(addResult => !addResult))
        {
            Logger.LogDebug("Block contains duplicates transaction");
            return Task.FromResult(false);
        }

        if (_blockchainService.GetChainId() != block.Header.ChainId)
        {
            Logger.LogDebug("Block chain id mismatch {ChainId}", block.Header.ChainId);
            return Task.FromResult(false);
        }

        if (block.Header.Height != AElfConstants.GenesisBlockHeight && !block.VerifySignature())
        {
            Logger.LogDebug("Block verify signature failed");
            return Task.FromResult(false);
        }

        if (block.Body.CalculateMerkleTreeRoot() != block.Header.MerkleTreeRootOfTransactions)
        {
            Logger.LogDebug("Block merkle tree root mismatch");
            return Task.FromResult(false);
        }

        if (block.Header.Height != AElfConstants.GenesisBlockHeight &&
            block.Header.Time.ToDateTime() - TimestampHelper.GetUtcNow().ToDateTime() >
            KernelConstants.AllowedFutureBlockTimeSpan.ToTimeSpan())
        {
            Logger.LogDebug("Future block received {Block}, {BlockTime}", block, block.Header.Time.ToDateTime());
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public async Task<bool> ValidateBlockBeforeExecuteAsync(IBlock block)
    {
        if (block?.Header == null || block.Body == null)
            return false;

        if (block.Body.TransactionsCount == 0)
            return false;

        // Verify that the transaction has been packaged in the current branch
        foreach (var transactionId in block.TransactionIds)
        {
            var blockIndexExists =
                await _transactionBlockIndexService.ValidateTransactionBlockIndexExistsInBranchAsync(transactionId,
                    block.Header.PreviousBlockHash);
            if (!blockIndexExists)
                continue;
            Logger.LogDebug("Transaction: {TransactionId} repackaged", transactionId.ToHex());
            return false;
        }

        return true;
    }

    public Task<bool> ValidateBlockAfterExecuteAsync(IBlock block)
    {
        return Task.FromResult(true);
    }
}