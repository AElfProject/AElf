using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ContractTestKit;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Miner;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.DependencyInjection;

namespace AElf.Contracts.Economic.TestBase;

//TODO: should inherit from base class, not a new executor 
public class EconomicTestTransactionExecutor : ITestTransactionExecutor
{
    private readonly IServiceProvider _serviceProvider;

    public EconomicTestTransactionExecutor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task<TransactionResult> ExecuteAsync(Transaction transaction)
    {
        var blockTimeProvider = _serviceProvider.GetRequiredService<IBlockTimeProvider>();
        var blockchainService = _serviceProvider.GetRequiredService<IBlockchainService>();
        var preBlock = await blockchainService.GetBestChainLastBlockHeaderAsync();
        var miningService = _serviceProvider.GetRequiredService<IMiningService>();
        var blockAttachService = _serviceProvider.GetRequiredService<IBlockAttachService>();

        var transactions = new List<Transaction> { transaction };
        var blockExecutedSet = await miningService.MineAsync(
            new RequestMiningDto
            {
                PreviousBlockHash = preBlock.GetHash(), PreviousBlockHeight = preBlock.Height,
                BlockExecutionTime = TimestampHelper.DurationFromMilliseconds(int.MaxValue),
                TransactionCountLimit = int.MaxValue
            }, transactions, blockTimeProvider.GetBlockTime());

        var block = blockExecutedSet.Block;

        await blockchainService.AddTransactionsAsync(transactions);
        await blockchainService.AddBlockAsync(block);
        await blockAttachService.AttachBlockAsync(block);

        return blockExecutedSet.TransactionResultMap[transaction.GetHash()];
    }

    public async Task<TransactionResult> ExecuteWithExceptionAsync(Transaction transaction)
    {
        var transactionResult = await ExecuteAsync(transaction);
        if (transactionResult.Status == TransactionResultStatus.Mined)
            throw new Exception($"Succeed to execute {transaction.MethodName}.");

        return transactionResult;
    }

    public async Task<ByteString> ReadAsync(Transaction transaction)
    {
        var blockchainService = _serviceProvider.GetRequiredService<IBlockchainService>();
        var transactionReadOnlyExecutionService =
            _serviceProvider.GetRequiredService<ITransactionReadOnlyExecutionService>();
        var blockTimeProvider = _serviceProvider.GetRequiredService<IBlockTimeProvider>();

        var preBlock = await blockchainService.GetBestChainLastBlockHeaderAsync();
        var transactionTrace = await transactionReadOnlyExecutionService.ExecuteAsync(new ChainContext
            {
                BlockHash = preBlock.GetHash(),
                BlockHeight = preBlock.Height
            },
            transaction,
            blockTimeProvider.GetBlockTime());

        return transactionTrace.ReturnValue;
    }

    public Task<StringValue> ReadWithExceptionAsync(Transaction transaction)
    {
        throw new NotImplementedException();
    }
}