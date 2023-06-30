using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestContract.BasicFunctionWithParallel;
using AElf.Cryptography;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Parallel;
using AElf.Kernel.SmartContract.Parallel.Domain;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.Token;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.TestBase;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AElf.Parallel.Tests;

public sealed class ParallelTests : AElfIntegratedTest<ParallelTestAElfModule>
{
    private readonly IAccountService _accountService;
    private readonly IBlockAttachService _blockAttachService;
    private readonly IBlockchainService _blockchainService;
    private readonly IBlockchainStateManager _blockchainStateManager;
    private readonly IBlockchainStateService _blockchainStateService;
    private readonly IBlockExecutingService _blockExecutingService;
    private readonly IBlockStateSetManger _blockStateSetManger;
    private readonly ITransactionGrouper _grouper;
    private readonly ILocalEventBus _localEventBus;
    private readonly INonparallelContractCodeProvider _nonparallelContractCodeProvider;
    private readonly ParallelTestHelper _parallelTestHelper;
    private readonly ISmartContractAddressService _smartContractAddressService;
    private readonly ITransactionResultManager _transactionResultManager;
    private readonly ITxHub _txHub;
    private readonly IStateStore<VersionedState> _versionedStates;

    private readonly int _groupCount = 10;
    private readonly int _transactionCount = 20;

    public ParallelTests()
    {
        _blockchainService = GetRequiredService<IBlockchainService>();
        _blockExecutingService = GetRequiredService<IBlockExecutingService>();
        _transactionResultManager = GetRequiredService<ITransactionResultManager>();
        _grouper = GetRequiredService<ITransactionGrouper>();
        _blockchainStateService = GetRequiredService<IBlockchainStateService>();
        _txHub = GetRequiredService<ITxHub>();
        _blockAttachService = GetRequiredService<IBlockAttachService>();
        _accountService = GetRequiredService<IAccountService>();
        _parallelTestHelper = GetRequiredService<ParallelTestHelper>();
        _smartContractAddressService = GetRequiredService<ISmartContractAddressService>();
        _blockchainStateManager = GetRequiredService<IBlockchainStateManager>();
        _versionedStates = GetRequiredService<IStateStore<VersionedState>>();
        _nonparallelContractCodeProvider = GetRequiredService<INonparallelContractCodeProvider>();
        _blockStateSetManger = GetRequiredService<IBlockStateSetManger>();
        _localEventBus = GetRequiredService<ILocalEventBus>();
    }

    [Fact]
    public async Task TokenTransferParallelTest()
    {
        var chain = await _blockchainService.GetChainAsync();
        var tokenAmount = _transactionCount / _groupCount;
        var (prepareTransactions, keyPairs) =
            await _parallelTestHelper.PrepareTokenForParallel(_groupCount, 1000000000);
        await _parallelTestHelper.BroadcastTransactions(prepareTransactions);
        var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight,
            prepareTransactions);
        block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, prepareTransactions)).Block;

        await _blockchainService.AddBlockAsync(block);
        await _blockAttachService.AttachBlockAsync(block);

        var systemTransactions = await _parallelTestHelper.GenerateTransferTransactions(1);
        var cancellableTransactions =
            await _parallelTestHelper.GenerateTransactionsWithoutConflictAsync(keyPairs, tokenAmount);
        var allTransaction = systemTransactions.Concat(cancellableTransactions).ToList();
        await _parallelTestHelper.BroadcastTransactions(allTransaction);

        var groupedTransactions = await _grouper.GroupAsync(
            new ChainContext { BlockHash = block.GetHash(), BlockHeight = block.Height },
            cancellableTransactions);
        groupedTransactions.Parallelizables.Count.ShouldBe(_groupCount);
        groupedTransactions.NonParallelizables.Count.ShouldBe(0);

        block = _parallelTestHelper.GenerateBlock(block.GetHash(), block.Height, allTransaction);
        block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, systemTransactions,
            cancellableTransactions, CancellationToken.None)).Block;
        await _blockchainService.AddBlockAsync(block);
        await _blockAttachService.AttachBlockAsync(block);

        var chainContext = new ChainContext
        {
            BlockHash = block.GetHash(),
            BlockHeight = block.Height
        };
        var tokenContractAddress =
            await _smartContractAddressService.GetAddressByContractNameAsync(chainContext,
                TokenSmartContractAddressNameProvider.StringName);
        var nonparallelContractCode =
            await _nonparallelContractCodeProvider.GetNonparallelContractCodeAsync(chainContext,
                tokenContractAddress);
        nonparallelContractCode.ShouldBeNull();

        groupedTransactions = await _grouper.GroupAsync(
            new ChainContext { BlockHash = block.GetHash(), BlockHeight = block.Height },
            cancellableTransactions);
        groupedTransactions.Parallelizables.Count.ShouldBe(_groupCount);
        groupedTransactions.NonParallelizables.Count.ShouldBe(0);

        block.TransactionIds.Count().ShouldBe(allTransaction.Count);
    }

    [Fact]
    public async Task TokenTransferFromParallelTest()
    {
        var chain = await _blockchainService.GetChainAsync();
        var tokenAmount = _transactionCount / _groupCount;
        var (prepareTransactions, keyPairs) =
            await _parallelTestHelper.PrepareTokenForParallel(_groupCount, 1000000000);
        await _parallelTestHelper.BroadcastTransactions(prepareTransactions);
        var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight,
            prepareTransactions);
        block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, prepareTransactions)).Block;

        await _blockchainService.AddBlockAsync(block);
        await _blockAttachService.AttachBlockAsync(block);

        var transactions = await _parallelTestHelper.GenerateApproveTransactions(keyPairs, 1000000000);
        await _parallelTestHelper.BroadcastTransactions(transactions);
        block = _parallelTestHelper.GenerateBlock(block.GetHash(), block.Height, transactions);
        block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;

        await _blockchainService.AddBlockAsync(block);
        await _blockAttachService.AttachBlockAsync(block);

        var systemTransactions = await _parallelTestHelper.GenerateTransferTransactions(1);
        var cancellableTransactions =
            await _parallelTestHelper.GenerateTransferFromTransactionsWithoutConflict(keyPairs, tokenAmount);
        await _parallelTestHelper.BroadcastTransactions(systemTransactions.Concat(cancellableTransactions));

        var groupedTransactions = await _grouper.GroupAsync(
            new ChainContext { BlockHash = block.GetHash(), BlockHeight = block.Height },
            cancellableTransactions);
        groupedTransactions.Parallelizables.Count.ShouldBe(1);
        groupedTransactions.NonParallelizables.Count.ShouldBe(0);

        block = _parallelTestHelper.GenerateBlock(block.GetHash(), block.Height,
            systemTransactions.Concat(cancellableTransactions));
        block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, systemTransactions,
            cancellableTransactions, CancellationToken.None)).Block;
        await _blockchainService.AddBlockAsync(block);
        await _blockAttachService.AttachBlockAsync(block);

        var chainContext = new ChainContext
        {
            BlockHash = block.GetHash(),
            BlockHeight = block.Height
        };
        var tokenContractAddress =
            await _smartContractAddressService.GetAddressByContractNameAsync(chainContext,
                TokenSmartContractAddressNameProvider.StringName);
        var nonparallelContractCode =
            await _nonparallelContractCodeProvider.GetNonparallelContractCodeAsync(chainContext, tokenContractAddress);
        nonparallelContractCode.ShouldBeNull();

        groupedTransactions = await _grouper.GroupAsync(
            new ChainContext { BlockHash = block.GetHash(), BlockHeight = block.Height },
            cancellableTransactions);
        groupedTransactions.Parallelizables.Count.ShouldBe(1);
        groupedTransactions.NonParallelizables.Count.ShouldBe(0);

        block.TransactionIds.Count().ShouldBe(systemTransactions.Count + cancellableTransactions.Count);

        systemTransactions = await _parallelTestHelper.GenerateTransferTransactions(1);
        cancellableTransactions =
            await _parallelTestHelper.GenerateTransferFromTransactionsWithoutConflictWithMultiSenderAsync(keyPairs,
                tokenAmount);
        await _parallelTestHelper.BroadcastTransactions(systemTransactions.Concat(cancellableTransactions));

        groupedTransactions = await _grouper.GroupAsync(
            new ChainContext { BlockHash = block.GetHash(), BlockHeight = block.Height },
            cancellableTransactions);
        groupedTransactions.Parallelizables.Count.ShouldBe(_groupCount);
        groupedTransactions.NonParallelizables.Count.ShouldBe(0);

        block = _parallelTestHelper.GenerateBlock(block.GetHash(), block.Height,
            systemTransactions.Concat(cancellableTransactions));
        block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, systemTransactions,
            cancellableTransactions, CancellationToken.None)).Block;

        await _blockchainService.AddBlockAsync(block);
        await _blockAttachService.AttachBlockAsync(block);

        nonparallelContractCode =
            await _nonparallelContractCodeProvider.GetNonparallelContractCodeAsync(
                new ChainContext { BlockHash = block.GetHash(), BlockHeight = block.Height },
                tokenContractAddress);
        nonparallelContractCode.ShouldBeNull();

        groupedTransactions = await _grouper.GroupAsync(
            new ChainContext { BlockHash = block.GetHash(), BlockHeight = block.Height },
            cancellableTransactions);
        groupedTransactions.Parallelizables.Count.ShouldBe(_groupCount);
        groupedTransactions.NonParallelizables.Count.ShouldBe(0);

        block.TransactionIds.Count().ShouldBe(systemTransactions.Count + cancellableTransactions.Count);
    }

    [Fact]
    public async Task TransferTwoSymbolParallelTest()
    {
        var symbol = "TELF";
        var keyPair = CryptoHelper.GenerateKeyPair();
        var address = Address.FromPublicKey(keyPair.PublicKey);
        await _parallelTestHelper.TransferTokenAsync(symbol, address);

        var transactionList = new List<Transaction>();
        var accountAddress = await _accountService.GetAccountAsync();

        var transferTransaction = await _parallelTestHelper.GenerateTransferTransaction(address,
            _parallelTestHelper.PrimaryTokenSymbol,
            100_00000000);
        transactionList.Add(transferTransaction);
        await _parallelTestHelper.BroadcastTransactions(transactionList);
        var block = await _parallelTestHelper.MinedOneBlock();

        var nonparallelContractCode =
            await _nonparallelContractCodeProvider.GetNonparallelContractCodeAsync(
                new ChainContext { BlockHash = block.GetHash(), BlockHeight = block.Height }, transferTransaction.To);
        nonparallelContractCode.ShouldBeNull();

        transactionList.Clear();
        transferTransaction =
            await _parallelTestHelper.GenerateTransferTransactionAsync(keyPair, accountAddress, symbol, 10);
        transactionList.Add(transferTransaction);
        transferTransaction = await _parallelTestHelper.GenerateTransferTransaction(address,
            _parallelTestHelper.PrimaryTokenSymbol, 10);
        transactionList.Add(transferTransaction);
        await _parallelTestHelper.BroadcastTransactions(transactionList);

        block = _parallelTestHelper.GenerateBlock(block.GetHash(), block.Height, transactionList);
        block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactionList)).Block;
        await _blockchainService.AddBlockAsync(block);
        await _blockAttachService.AttachBlockAsync(block);
        var transactionResults = await GetTransactionResultsAsync(block.Body.TransactionIds.ToList(), block.Header);
        transactionResults.Count.ShouldBe(2);
        transactionResults.Count(t => t.Status == TransactionResultStatus.Mined).ShouldBe(2);

        nonparallelContractCode =
            await _nonparallelContractCodeProvider.GetNonparallelContractCodeAsync(
                new ChainContext { BlockHash = block.GetHash(), BlockHeight = block.Height }, transferTransaction.To);
        nonparallelContractCode.ShouldBeNull();
    }

    [Fact]
    public async Task WrongParallelTest()
    {
        var chain = await _blockchainService.GetChainAsync();
        await _blockchainService.SetIrreversibleBlockAsync(chain, chain.BestChainHeight, chain.BestChainHash);

        //prepare token for tx verify
        var (tokenTransactions, groupUsers) =
            await _parallelTestHelper.PrepareTokenForParallel(_groupCount, 1000_00000000);
        await _parallelTestHelper.BroadcastTransactions(tokenTransactions);
        var prepareBlock =
            _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, tokenTransactions);
        prepareBlock =
            (await _blockExecutingService.ExecuteBlockAsync(prepareBlock.Header,
                tokenTransactions)).Block;
        await _blockchainService.AddBlockAsync(prepareBlock);
        await _blockAttachService.AttachBlockAsync(prepareBlock);

        chain = await _blockchainService.GetChainAsync();

        var transactions =
            _parallelTestHelper.GenerateBasicFunctionWithParallelTransactions(groupUsers, _transactionCount);
        var transferTransaction = await _parallelTestHelper.GenerateTransferTransaction(
            Address.FromPublicKey(groupUsers[1].PublicKey)
            , "ELF", 10);
        transactions.Add(transferTransaction);
        await _parallelTestHelper.BroadcastTransactions(transactions);

        var otherTransactions =
            _parallelTestHelper.GenerateBasicFunctionWithParallelTransactions(groupUsers, _transactionCount);
        var otherTransferTransaction = await _parallelTestHelper.GenerateTransferTransaction(
            Address.FromPublicKey(groupUsers[2].PublicKey)
            , "ELF", 10);
        otherTransactions.Add(otherTransferTransaction);
        await _parallelTestHelper.BroadcastTransactions(otherTransactions);

        var transferTransactions = await _parallelTestHelper.GenerateTransferTransactions(16);
        await _parallelTestHelper.BroadcastTransactions(transferTransactions);

        var poolSize = (await _txHub.GetTransactionPoolStatusAsync()).AllTransactionCount;
        poolSize.ShouldBe(transactions.Count * 2 + transferTransactions.Count);

        var groupedTransactions = await _grouper.GroupAsync(
            new ChainContext { BlockHash = chain.BestChainHash, BlockHeight = chain.BestChainHeight },
            transactions);
        groupedTransactions.Parallelizables.Count.ShouldBe(_transactionCount + 1);
        groupedTransactions.NonParallelizables.Count.ShouldBe(0);
        for (var i = 0; i < transactions.Count; i++)
            transactions[i].GetHash().ShouldBe(groupedTransactions.Parallelizables[i][0].GetHash());

        var otherGroupedTransactions = await _grouper.GroupAsync(
            new ChainContext { BlockHash = chain.BestChainHash, BlockHeight = chain.BestChainHeight },
            otherTransactions);
        otherGroupedTransactions.Parallelizables.Count.ShouldBe(_transactionCount + 1);
        otherGroupedTransactions.NonParallelizables.Count.ShouldBe(0);

        var groupedTransferTransactions = await _grouper.GroupAsync(
            new ChainContext { BlockHash = chain.BestChainHash, BlockHeight = chain.BestChainHeight },
            transferTransactions);
        groupedTransferTransactions.Parallelizables.Count.ShouldBe(1);
        groupedTransferTransactions.Parallelizables[0].Count.ShouldBe(transferTransactions.Count);
        groupedTransferTransactions.NonParallelizables.Count.ShouldBe(0);

        _localEventBus.Subscribe<ConflictingTransactionsFoundInParallelGroupsEvent>(e =>
        {
            e.ConflictingSets.Count.ShouldBe(_groupCount + 1);
            e.ExistingSets.Count.ShouldBe(_groupCount);
            return Task.CompletedTask;
        });

        var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, transactions);
        block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, transactions)).Block;
        block.TransactionIds.Count().ShouldBe(_transactionCount + 1);

        var transactionResults = await GetTransactionResultsAsync(block.Body.TransactionIds.ToList(), block.Header);
        transactionResults.Count(t => t.Status == TransactionResultStatus.Mined).ShouldBe(_groupCount);
        transactionResults.Count(t => t.Status == TransactionResultStatus.Conflict).ShouldBe(_groupCount + 1);
        await _blockchainService.AddBlockAsync(block);
        await _blockAttachService.AttachBlockAsync(block);
        var accountAddress = await _accountService.GetAccountAsync();

        var chainContext = new ChainContext
        {
            BlockHash = block.GetHash(),
            BlockHeight = block.Height
        };
        var tokenContractAddress =
            await _smartContractAddressService.GetAddressByContractNameAsync(chainContext,
                TokenSmartContractAddressNameProvider.StringName);
        var nonparallelContractCode =
            await _nonparallelContractCodeProvider.GetNonparallelContractCodeAsync(chainContext,
                tokenContractAddress);
        nonparallelContractCode.ShouldBeNull();

        foreach (var transaction in transactions)
        {
            if (transaction.To == tokenContractAddress) continue;
            var param = IncreaseWinMoneyInput.Parser.ParseFrom(transaction.Params);
            var input = new QueryTwoUserWinMoneyInput
            {
                First = param.First,
                Second = param.Second
            };
            var queryTransaction = _parallelTestHelper.GenerateTransaction(accountAddress,
                ParallelTestHelper.BasicFunctionWithParallelContractAddress,
                nameof(BasicFunctionWithParallelContract.QueryTwoUserWinMoney), input);
            var byteString =
                await _parallelTestHelper.ExecuteReadOnlyAsync(queryTransaction, block.GetHash(), block.Height);
            var output = TwoUserMoneyOut.Parser.ParseFrom(byteString);
            output.FirstInt64Value.ShouldBe(1);
            var result = transactionResults.First(t => t.TransactionId == transaction.GetHash());
            if (result.Status == TransactionResultStatus.Mined)
                output.SecondInt64Value.ShouldBe(1);
            else if (result.Status == TransactionResultStatus.Conflict) output.SecondInt64Value.ShouldBe(0);
        }

        nonparallelContractCode =
            await _nonparallelContractCodeProvider.GetNonparallelContractCodeAsync(new ChainContext
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height
            }, ParallelTestHelper.BasicFunctionWithParallelContractAddress);
        nonparallelContractCode.CodeHash.ShouldBe(
            HashHelper.ComputeFrom(_parallelTestHelper.BasicFunctionWithParallelContractCode));

        var blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
        blockStateSet.Changes.Count.ShouldBeGreaterThan(0);
        var blockExecutedData = blockStateSet.BlockExecutedData.First();
        var versionedState = await _versionedStates.GetAsync(blockExecutedData.Key);
        versionedState.ShouldBeNull();

        await _blockchainStateService.MergeBlockStateAsync(block.Height, block.GetHash());
        blockStateSet = await _blockStateSetManger.GetBlockStateSetAsync(block.GetHash());
        blockStateSet.ShouldBeNull();

        nonparallelContractCode =
            await _nonparallelContractCodeProvider.GetNonparallelContractCodeAsync(new ChainContext
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height
            }, ParallelTestHelper.BasicFunctionWithParallelContractAddress);
        nonparallelContractCode.CodeHash.ShouldBe(
            HashHelper.ComputeFrom(_parallelTestHelper.BasicFunctionWithParallelContractCode));

        versionedState = await _versionedStates.GetAsync(blockExecutedData.Key);
        versionedState.Key.ShouldBe(blockExecutedData.Key);
        versionedState.Value.ShouldBe(blockExecutedData.Value);

        groupedTransactions = await _grouper.GroupAsync(
            new ChainContext { BlockHash = block.GetHash(), BlockHeight = block.Height },
            transactions);

        groupedTransactions.Parallelizables.Count.ShouldBe(1);
        groupedTransactions.Parallelizables[0][0].To.ShouldBe(tokenContractAddress);
        groupedTransactions.NonParallelizables.Count.ShouldBe(_transactionCount);

        otherGroupedTransactions = await _grouper.GroupAsync(
            new ChainContext { BlockHash = block.GetHash(), BlockHeight = block.Height },
            otherTransactions);
        otherGroupedTransactions.Parallelizables.Count.ShouldBe(1);
        otherGroupedTransactions.Parallelizables[0][0].To.ShouldBe(tokenContractAddress);
        otherGroupedTransactions.NonParallelizables.Count.ShouldBe(_transactionCount);

        groupedTransferTransactions = await _grouper.GroupAsync(
            new ChainContext { BlockHash = chain.BestChainHash, BlockHeight = chain.BestChainHeight },
            transferTransactions);
        groupedTransferTransactions.Parallelizables.Count.ShouldBe(1);
        groupedTransferTransactions.Parallelizables[0].Count.ShouldBe(transferTransactions.Count);
        groupedTransferTransactions.NonParallelizables.Count.ShouldBe(0);

        poolSize = (await _txHub.GetTransactionPoolStatusAsync()).AllTransactionCount;

        poolSize.ShouldBe(transactions.Count * 2 + transferTransactions.Count - block.TransactionIds.Count());
    }

    [Fact]
    public async Task Parallel_TransactionWithoutContract()
    {
        var chain = await _blockchainService.GetChainAsync();
        var tokenAmount = _transactionCount / _groupCount;
        var accountAddress = await _accountService.GetAccountAsync();

        var (prepareTransactions, keyPairs) = await _parallelTestHelper.PrepareTokenForParallel(_groupCount, 100000000);

        var transactionWithoutContract = _parallelTestHelper.GenerateTransaction(accountAddress,
            SampleAddress.AddressList[0], "Transfer", new Empty());
        var transactionHash = transactionWithoutContract.GetHash();
        var signature = await _accountService.SignAsync(transactionHash.ToByteArray());
        transactionWithoutContract.Signature = ByteString.CopyFrom(signature);
        prepareTransactions.Add(transactionWithoutContract);

        var cancellableTransaction = _parallelTestHelper.GenerateTransaction(accountAddress,
            ParallelTestHelper.BasicFunctionWithParallelContractAddress, "QueryWinMoney",
            new Empty());
        signature = await _accountService.SignAsync(cancellableTransaction.GetHash().ToByteArray());
        cancellableTransaction.Signature = ByteString.CopyFrom(signature);
        var cancellableTransactions = new List<Transaction> { cancellableTransaction };
        var allTransactions = prepareTransactions.Concat(cancellableTransactions).ToList();

        await _parallelTestHelper.BroadcastTransactions(allTransactions);

        var groupedTransactions = await _grouper.GroupAsync(
            new ChainContext { BlockHash = chain.BestChainHash, BlockHeight = chain.BestChainHeight },
            prepareTransactions);
        groupedTransactions.Parallelizables.Count.ShouldBe(1);
        groupedTransactions.NonParallelizables.Count.ShouldBe(0);
        groupedTransactions.TransactionsWithoutContract.Count.ShouldBe(1);
        var transaction = groupedTransactions.TransactionsWithoutContract[0];
        transaction.GetHash().ShouldBe(transactionHash);

        var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, allTransactions);
        block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, prepareTransactions,
            cancellableTransactions, CancellationToken.None)).Block;
        await _blockchainService.AddTransactionsAsync(allTransactions);
        await _blockchainService.AddBlockAsync(block);
        await _blockAttachService.AttachBlockAsync(block);

        var transactionResult =
            await _transactionResultManager.GetTransactionResultAsync(transactionHash,
                block.Header.GetHash());
        transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        transactionResult.Error.ShouldContain("Invalid contract address");

        var systemTransactions = await _parallelTestHelper.GenerateTransferTransactions(1);
        cancellableTransactions =
            await _parallelTestHelper.GenerateTransactionsWithoutConflictAsync(keyPairs, tokenAmount);

        transactionWithoutContract = _parallelTestHelper.GenerateTransaction(accountAddress,
            SampleAddress.AddressList[0], "Transfer", new Empty());
        transactionHash = transactionWithoutContract.GetHash();
        signature = await _accountService.SignAsync(transactionHash.ToByteArray());
        transactionWithoutContract.Signature = ByteString.CopyFrom(signature);
        cancellableTransactions.Add(transactionWithoutContract);

        var allTransaction = systemTransactions.Concat(cancellableTransactions).ToList();
        await _parallelTestHelper.BroadcastTransactions(allTransaction);

        groupedTransactions = await _grouper.GroupAsync(
            new ChainContext { BlockHash = block.GetHash(), BlockHeight = block.Height },
            cancellableTransactions);
        groupedTransactions.Parallelizables.Count.ShouldBe(_groupCount);
        groupedTransactions.NonParallelizables.Count.ShouldBe(0);
        groupedTransactions.TransactionsWithoutContract.Count.ShouldBe(1);
        transaction = groupedTransactions.TransactionsWithoutContract[0];
        transaction.GetHash().ShouldBe(transactionHash);

        block = _parallelTestHelper.GenerateBlock(block.GetHash(), block.Height, allTransaction);
        block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, systemTransactions,
            cancellableTransactions, CancellationToken.None)).Block;
        await _blockchainService.AddTransactionsAsync(allTransactions);
        await _blockchainService.AddBlockAsync(block);
        await _blockAttachService.AttachBlockAsync(block);

        var chainContext = new ChainContext
        {
            BlockHash = block.GetHash(),
            BlockHeight = block.Height
        };
        var tokenContractAddress =
            await _smartContractAddressService.GetAddressByContractNameAsync(chainContext,
                TokenSmartContractAddressNameProvider.StringName);
        var nonparallelContractCode = await _nonparallelContractCodeProvider.GetNonparallelContractCodeAsync(
            chainContext, tokenContractAddress);
        nonparallelContractCode.ShouldBeNull();

        groupedTransactions = await _grouper.GroupAsync(
            new ChainContext { BlockHash = block.GetHash(), BlockHeight = block.Height },
            cancellableTransactions);
        groupedTransactions.Parallelizables.Count.ShouldBe(_groupCount);
        groupedTransactions.NonParallelizables.Count.ShouldBe(0);

        block.TransactionIds.Count().ShouldBe(allTransaction.Count);
        block.TransactionIds.ShouldContain(transactionHash);

        transactionResult =
            await _transactionResultManager.GetTransactionResultAsync(transactionHash,
                block.Header.GetHash());
        transactionResult.Status.ShouldBe(TransactionResultStatus.Failed);
        transactionResult.Error.ShouldContain("Invalid contract address");
    }

    [Fact]
    public async Task Use_Same_Resource_Key_With_SystemTransaction_Test()
    {
        var chain = await _blockchainService.GetChainAsync();
        var accountAddress = await _accountService.GetAccountAsync();
        var startBalance = await _parallelTestHelper.QueryBalanceAsync(accountAddress, "ELF", chain.BestChainHash,
            chain.BestChainHeight);
        var systemTransactions = await _parallelTestHelper.GenerateTransferTransactions(1);
        var cancellableTransactions = await _parallelTestHelper.GenerateTransferTransactions(1);
        var allTransactions = systemTransactions.Concat(cancellableTransactions).ToList();
        await _parallelTestHelper.BroadcastTransactions(allTransactions);
        var block = _parallelTestHelper.GenerateBlock(chain.BestChainHash, chain.BestChainHeight, allTransactions);
        block = (await _blockExecutingService.ExecuteBlockAsync(block.Header, systemTransactions,
            cancellableTransactions, CancellationToken.None)).Block;

        var transactionResults = await GetTransactionResultsAsync(block.Body.TransactionIds.ToList(), block.Header);
        var totalFee = transactionResults
            .Select(transactionResult =>
                transactionResult.Logs.Single(l => l.Name == nameof(TransactionFeeCharged)))
            .Select(relatedLog => TransactionFeeCharged.Parser.ParseFrom(relatedLog.NonIndexed))
            .Where(chargedEvent => chargedEvent.Symbol == "ELF").Sum(chargedEvent => chargedEvent.Amount);
        var amount = allTransactions.Sum(t => TransferInput.Parser.ParseFrom(t.Params).Amount);
        var endBalance =
            await _parallelTestHelper.QueryBalanceAsync(accountAddress, "ELF", block.GetHash(), block.Height);
        (startBalance - endBalance).ShouldBe(amount + totalFee);
    }

    private async Task<List<TransactionResult>> GetTransactionResultsAsync(List<Hash> transactionIds,
        BlockHeader blockHeader)
    {
        var transactionResults = new List<TransactionResult>();
        foreach (var transactionId in transactionIds)
        {
            var result =
                await _transactionResultManager.GetTransactionResultAsync(transactionId,
                    blockHeader.GetDisambiguatingHash());
            if (result != null) transactionResults.Add(result);
        }

        return transactionResults;
    }
}