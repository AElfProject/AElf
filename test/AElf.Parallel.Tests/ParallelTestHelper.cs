using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ContractDeployer;
using AElf.Contracts.MultiToken;
using AElf.Contracts.TestContract.BasicFunctionWithParallel;
using AElf.Cryptography;
using AElf.Cryptography.ECDSA;
using AElf.Kernel;
using AElf.Kernel.Account.Application;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Infrastructure;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.Token;
using AElf.Kernel.TransactionPool.Application;
using AElf.OS;
using AElf.OS.Node.Application;
using AElf.Types;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Volo.Abp.Threading;

namespace AElf.Parallel.Tests;

public class ParallelTestHelper : OSTestHelper
{
    private readonly IAccountService _accountService;
    private readonly ISmartContractAddressService _smartContractAddressService;
    private readonly IStaticChainInformationProvider _staticChainInformationProvider;
    private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
    public readonly string PrimaryTokenSymbol = "ELF";
    private IReadOnlyDictionary<string, byte[]> _codes;

    public ParallelTestHelper(IOsBlockchainNodeContextService osBlockchainNodeContextService,
        IAccountService accountService,
        IMinerService minerService,
        IBlockchainService blockchainService,
        ISmartContractAddressService smartContractAddressService,
        IBlockAttachService blockAttachService,
        IStaticChainInformationProvider staticChainInformationProvider,
        ITransactionResultService transactionResultService,
        IOptionsSnapshot<ChainOptions> chainOptions,
        ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
        ITransactionPoolService transactionPoolService
    ) : base(osBlockchainNodeContextService, accountService,
        minerService, blockchainService, smartContractAddressService, blockAttachService,
        staticChainInformationProvider, transactionResultService, chainOptions, transactionPoolService)
    {
        _accountService = accountService;
        _staticChainInformationProvider = staticChainInformationProvider;
        _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
        _smartContractAddressService = smartContractAddressService;
    }

    public new IReadOnlyDictionary<string, byte[]> Codes =>
        _codes ?? (_codes = ContractsDeployer.GetContractCodes<ParallelTestHelper>());

    public byte[] BasicFunctionWithParallelContractCode =>
        Codes.Single(kv => kv.Key.Split(",").First().Trim().EndsWith("BasicFunctionWithParallel")).Value;

    public static Address BasicFunctionWithParallelContractAddress { get; private set; }

    public override Block GenerateBlock(Hash preBlockHash, long preBlockHeight,
        IEnumerable<Transaction> transactions = null)
    {
        var block = new Block
        {
            Header = new BlockHeader
            {
                ChainId = _staticChainInformationProvider.ChainId,
                Height = preBlockHeight + 1,
                PreviousBlockHash = preBlockHash,
                Time = TimestampHelper.GetUtcNow(),
                SignerPubkey = ByteString.CopyFrom(AsyncHelper.RunSync(_accountService.GetPublicKeyAsync))
            },
            Body = new BlockBody()
        };
        if (transactions != null)
            foreach (var transaction in transactions)
                block.AddTransaction(transaction);

        return block;
    }

    public async Task DeployBasicFunctionWithParallelContract()
    {
        BasicFunctionWithParallelContractAddress = await DeployContract<BasicFunctionWithParallelContract>();
    }

    public List<Transaction> GenerateBasicFunctionWithParallelTransactions(List<ECKeyPair> groupUsers,
        int transactionCount)
    {
        var transactions = new List<Transaction>();

        var groupCount = groupUsers.Count;
        for (var i = 0; i < groupCount; i++)
        {
            var keyPair = groupUsers[i];
            var from = Address.FromPublicKey(keyPair.PublicKey);
            var count = transactionCount / groupCount;
            for (var j = 0; j < count; j++)
            {
                var address = Address.FromPublicKey(CryptoHelper.GenerateKeyPair().PublicKey);
                var transaction = GenerateTransaction(from,
                    BasicFunctionWithParallelContractAddress,
                    nameof(BasicFunctionWithParallelContractContainer.BasicFunctionWithParallelContractStub
                        .IncreaseWinMoney),
                    new IncreaseWinMoneyInput { First = from, Second = address });
                var signature =
                    CryptoHelper.SignWithPrivateKey(keyPair.PrivateKey, transaction.GetHash().ToByteArray());
                transaction.Signature = ByteString.CopyFrom(signature);

                transactions.Add(transaction);
            }
        }

        return transactions;
    }

    public async Task<List<Transaction>> GenerateTransferFromTransactionsWithoutConflictWithMultiSenderAsync(
        List<ECKeyPair> keyPairs, int count = 1)
    {
        var transactions = new List<Transaction>();
        foreach (var keyPair in keyPairs)
        {
            var from = Address.FromPublicKey(keyPair.PublicKey);
            for (var i = 0; i < count; i++)
            {
                var to = CryptoHelper.GenerateKeyPair();
                var transaction = GenerateTransaction(from,
                    await _smartContractAddressService.GetAddressByContractNameAsync(await GetChainContextAsync(),
                        TokenSmartContractAddressNameProvider.StringName),
                    nameof(TokenContractContainer.TokenContractStub.TransferFrom),
                    new TransferFromInput
                        { From = from, To = Address.FromPublicKey(to.PublicKey), Amount = 1, Symbol = "ELF" });
                var signature =
                    CryptoHelper.SignWithPrivateKey(keyPair.PrivateKey, transaction.GetHash().ToByteArray());
                transaction.Signature = ByteString.CopyFrom(signature);

                transactions.Add(transaction);
            }
        }

        return transactions;
    }

    public async Task<ByteString> ExecuteReadOnlyAsync(Transaction transaction, Hash blockHash, long blockHeight)
    {
        var transactionTrace = await _transactionReadOnlyExecutionService.ExecuteAsync(new ChainContext
            {
                BlockHash = blockHash,
                BlockHeight = blockHeight
            },
            transaction,
            DateTime.UtcNow.ToTimestamp());

        return transactionTrace.ReturnValue;
    }

    public async Task<long> QueryBalanceAsync(Address address, string symbol, Hash blockHash, long blockHeight)
    {
        var accountAddress = await _accountService.GetAccountAsync();
        var transaction = GenerateTransaction(accountAddress,
            await _smartContractAddressService.GetAddressByContractNameAsync(await GetChainContextAsync(),
                TokenSmartContractAddressNameProvider.StringName),
            nameof(TokenContractContainer.TokenContractStub.GetBalance),
            new GetBalanceInput { Owner = address, Symbol = symbol });
        var returnValue = await ExecuteReadOnlyAsync(transaction, blockHash, blockHeight);
        return GetBalanceOutput.Parser.ParseFrom(returnValue).Balance;
    }

    public async Task TransferTokenAsync(string symbol, Address issueAddress)
    {
        var ownAddress = await _accountService.GetAccountAsync();
        var tokenContractAddress =
            await _smartContractAddressService.GetAddressByContractNameAsync(await GetChainContextAsync(),
                TokenSmartContractAddressNameProvider.StringName);
        
        var issueTokenTransaction = GenerateTransaction(ownAddress, tokenContractAddress,
            nameof(TokenContractContainer.TokenContractStub.Transfer), new TransferInput
            {
                Symbol = symbol,
                Amount = 100_000000,
                To = issueAddress,
                Memo = "Transfer"
            });
        var signature = await _accountService.SignAsync(issueTokenTransaction.GetHash().ToByteArray());
        issueTokenTransaction.Signature = ByteString.CopyFrom(signature);
        await BroadcastTransactions(new List<Transaction> { issueTokenTransaction });
        await MinedOneBlock();
    }

    public async Task<Transaction> GenerateTransferTransactionAsync(ECKeyPair fromKeyPair, Address to, string symbol,
        long amount)
    {
        var transaction = GenerateTransaction(Address.FromPublicKey(fromKeyPair.PublicKey),
            await _smartContractAddressService.GetAddressByContractNameAsync(await GetChainContextAsync(),
                TokenSmartContractAddressNameProvider.StringName),
            nameof(TokenContractContainer.TokenContractStub.Transfer),
            new TransferInput { To = to, Amount = amount, Symbol = symbol });

        var signature =
            CryptoHelper.SignWithPrivateKey(fromKeyPair.PrivateKey, transaction.GetHash().ToByteArray());
        transaction.Signature = ByteString.CopyFrom(signature);

        return transaction;
    }

    public async Task<Transaction> GenerateTransferTransaction(Address to, string symbol, long amount)
    {
        var fromAddress = await _accountService.GetAccountAsync();
        var transaction = GenerateTransaction(fromAddress,
            await _smartContractAddressService.GetAddressByContractNameAsync(await GetChainContextAsync(),
                TokenSmartContractAddressNameProvider.StringName),
            nameof(TokenContractContainer.TokenContractStub.Transfer),
            new TransferInput { To = to, Amount = amount, Symbol = symbol });

        var signature = await _accountService.SignAsync(transaction.GetHash().ToByteArray());
        transaction.Signature = ByteString.CopyFrom(signature);

        return transaction;
    }
}