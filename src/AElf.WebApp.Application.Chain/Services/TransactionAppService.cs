using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.FeeCalculation.Extensions;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.TransactionPool;
using AElf.Types;
using AElf.WebApp.Application.Chain.Dto;
using AElf.WebApp.Application.Chain.Infrastructure;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.EventBus.Local;
using Volo.Abp.ObjectMapping;

namespace AElf.WebApp.Application.Chain;

public interface ITransactionAppService
{
    Task<string> ExecuteTransactionAsync(ExecuteTransactionDto input);

    Task<string> ExecuteRawTransactionAsync(ExecuteRawTransactionDto input);

    Task<CreateRawTransactionOutput> CreateRawTransactionAsync(CreateRawTransactionInput input);

    Task<SendRawTransactionOutput> SendRawTransactionAsync(SendRawTransactionInput input);

    Task<SendTransactionOutput> SendTransactionAsync(SendTransactionInput input);

    Task<string[]> SendTransactionsAsync(SendTransactionsInput input);

    Task<CalculateTransactionFeeOutput> CalculateTransactionFeeAsync(CalculateTransactionFeeInput input);
}

public class TransactionAppService : AElfAppService, ITransactionAppService
{
    private readonly IBlockchainService _blockchainService;
    private readonly IObjectMapper<ChainApplicationWebAppAElfModule> _objectMapper;
    private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
    private readonly ITransactionResultStatusCacheProvider _transactionResultStatusCacheProvider;
    private readonly IPlainTransactionExecutingService _plainTransactionExecutingService;
    private readonly WebAppOptions _webAppOptions;


    public TransactionAppService(ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
        IBlockchainService blockchainService, IObjectMapper<ChainApplicationWebAppAElfModule> objectMapper,
        ITransactionResultStatusCacheProvider transactionResultStatusCacheProvider,
        IPlainTransactionExecutingService plainTransactionExecutingService,
        IOptionsMonitor<WebAppOptions> webAppOptions)
    {
        _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
        _blockchainService = blockchainService;
        _objectMapper = objectMapper;
        _transactionResultStatusCacheProvider = transactionResultStatusCacheProvider;
        _plainTransactionExecutingService = plainTransactionExecutingService;
        _webAppOptions = webAppOptions.CurrentValue;

        LocalEventBus = NullLocalEventBus.Instance;
        Logger = NullLogger<TransactionAppService>.Instance;
    }

    public ILocalEventBus LocalEventBus { get; set; }

    public ILogger<TransactionAppService> Logger { get; set; }

    /// <summary>
    ///     Call a read-only method on a contract.
    /// </summary>
    /// <returns></returns>
    public async Task<string> ExecuteTransactionAsync(ExecuteTransactionDto input)
    {
        Transaction transaction;

        try
        {
            var byteArray = ByteArrayHelper.HexStringToByteArray(input.RawTransaction);
            transaction = Transaction.Parser.ParseFrom(byteArray);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "{ErrorMessage}", e.Message); //for debug
            throw new UserFriendlyException(Error.Message[Error.InvalidParams],
                Error.InvalidParams.ToString());
        }

        if (!transaction.VerifySignature())
            throw new UserFriendlyException(Error.Message[Error.InvalidSignature],
                Error.InvalidSignature.ToString());

        try
        {
            var response = await CallReadOnlyAsync(transaction);
            return response?.ToHex();
        }
        catch (Exception e)
        {
            using var detail = new StringReader(e.Message);
            throw new UserFriendlyException(Error.Message[Error.InvalidTransaction],
                Error.InvalidTransaction.ToString(), await detail.ReadLineAsync());
        }
    }

    public async Task<string> ExecuteRawTransactionAsync(ExecuteRawTransactionDto input)
    {
        Transaction transaction;

        try
        {
            var byteArray = ByteArrayHelper.HexStringToByteArray(input.RawTransaction);
            transaction = Transaction.Parser.ParseFrom(byteArray);
            transaction.Signature = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(input.Signature));
        }
        catch (Exception e)
        {
            Logger.LogError(e, "{ErrorMessage}", e.Message); //for debug
            throw new UserFriendlyException(Error.Message[Error.InvalidParams],
                Error.InvalidParams.ToString());
        }

        if (!transaction.VerifySignature())
            throw new UserFriendlyException(Error.Message[Error.InvalidSignature],
                Error.InvalidSignature.ToString());

        try
        {
            var response = await CallReadOnlyAsync(transaction);
            try
            {
                var contractMethodDescriptor =
                    await GetContractMethodDescriptorAsync(transaction.To, transaction.MethodName);
                var output = contractMethodDescriptor.OutputType.Parser.ParseFrom(ByteString.CopyFrom(response));
                return JsonFormatter.ToDiagnosticString(output);
            }
            catch
            {
                return response?.ToHex();
            }
        }
        catch (Exception e)
        {
            using var detail = new StringReader(e.Message);
            throw new UserFriendlyException(Error.Message[Error.InvalidTransaction],
                Error.InvalidTransaction.ToString(), await detail.ReadLineAsync());
        }
    }

    /// <summary>
    ///     Creates an unsigned serialized transaction
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task<CreateRawTransactionOutput> CreateRawTransactionAsync(CreateRawTransactionInput input)
    {
        var transaction = new Transaction
        {
            From = Address.FromBase58(input.From),
            To = Address.FromBase58(input.To),
            RefBlockNumber = input.RefBlockNumber,
            RefBlockPrefix = BlockHelper.GetRefBlockPrefix(Hash.LoadFromHex(input.RefBlockHash)),
            MethodName = input.MethodName
        };
        var methodDescriptor = await GetContractMethodDescriptorAsync(Address.FromBase58(input.To), input.MethodName);
        if (methodDescriptor == null)
            throw new UserFriendlyException(Error.Message[Error.NoMatchMethodInContractAddress],
                Error.NoMatchMethodInContractAddress.ToString());
        try
        {
            var parameters = methodDescriptor.InputType.Parser.ParseJson(input.Params);
            if (!IsValidMessage(parameters))
                throw new UserFriendlyException(Error.Message[Error.InvalidParams], Error.InvalidParams.ToString());
            transaction.Params = parameters.ToByteString();
        }
        catch
        {
            throw new UserFriendlyException(Error.Message[Error.InvalidParams], Error.InvalidParams.ToString());
        }

        return new CreateRawTransactionOutput
        {
            RawTransaction = transaction.ToByteArray().ToHex()
        };
    }

    /// <summary>
    ///     send a transaction
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public async Task<SendRawTransactionOutput> SendRawTransactionAsync(SendRawTransactionInput input)
    {
        var transaction = Transaction.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(input.Transaction));
        transaction.Signature = ByteString.CopyFrom(ByteArrayHelper.HexStringToByteArray(input.Signature));
        var txIds = await PublishTransactionsAsync(new[] { transaction.ToByteArray().ToHex() });

        var output = new SendRawTransactionOutput
        {
            TransactionId = txIds[0]
        };

        if (!input.ReturnTransaction) return output;

        var transactionDto = _objectMapper.Map<Transaction, TransactionDto>(transaction);
        var contractMethodDescriptor =
            await GetContractMethodDescriptorAsync(transaction.To, transaction.MethodName);
        if (contractMethodDescriptor == null)
            throw new UserFriendlyException(Error.Message[Error.NoMatchMethodInContractAddress],
                Error.NoMatchMethodInContractAddress.ToString());

        var parameters = contractMethodDescriptor.InputType.Parser.ParseFrom(transaction.Params);
        if (!IsValidMessage(parameters))
            throw new UserFriendlyException(Error.Message[Error.InvalidParams], Error.InvalidParams.ToString());

        transactionDto.Params = JsonFormatter.ToDiagnosticString(parameters);
        output.Transaction = transactionDto;

        return output;
    }

    /// <summary>
    ///     Broadcast a transaction
    /// </summary>
    /// <returns></returns>
    public async Task<SendTransactionOutput> SendTransactionAsync(SendTransactionInput input)
    {
        var txIds = await PublishTransactionsAsync(new[] { input.RawTransaction });
        return new SendTransactionOutput
        {
            TransactionId = txIds[0]
        };
    }

    /// <summary>
    ///     Broadcast multiple transactions
    /// </summary>
    /// <returns></returns>
    public async Task<string[]> SendTransactionsAsync(SendTransactionsInput input)
    {
        var txIds = await PublishTransactionsAsync(input.RawTransactions.Split(","));

        return txIds;
    }

    public async Task<CalculateTransactionFeeOutput> CalculateTransactionFeeAsync(CalculateTransactionFeeInput input)
    {
        Transaction transaction;

        try
        {
            var byteArray = ByteArrayHelper.HexStringToByteArray(input.RawTransaction);
            transaction = Transaction.Parser.ParseFrom(byteArray);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "{ErrorMessage}", e.Message); //for debug
            throw new UserFriendlyException(Error.Message[Error.InvalidParams],
                Error.InvalidParams.ToString());
        }

        try
        {
            var result = await EstimateTransactionFee(transaction);
            return result;
        }
        catch (Exception e)
        {
            using var detail = new StringReader(e.Message);
            throw new UserFriendlyException(Error.Message[Error.InvalidTransaction],
                Error.InvalidTransaction.ToString(), await detail.ReadLineAsync());
        }
    }

    private async Task<CalculateTransactionFeeOutput> EstimateTransactionFee(Transaction transaction)
    {
        var chainContext = await GetChainContextAsync();
        var executionReturnSets = await _plainTransactionExecutingService.ExecuteAsync(new TransactionExecutingDto
        {
            Transactions = new[] { transaction },
            BlockHeader = new BlockHeader
            {
                PreviousBlockHash = chainContext.BlockHash,
                Height = chainContext.BlockHeight,
                Time = TimestampHelper.GetUtcNow()
            }
        }, CancellationToken.None);
        var result = new CalculateTransactionFeeOutput();
        if (executionReturnSets.FirstOrDefault()?.Status == TransactionResultStatus.Mined)
        {
            var transactionFees =
                executionReturnSets.FirstOrDefault()?.TransactionResult.GetChargedTransactionFees();
            var resourceFees = executionReturnSets.FirstOrDefault()?.TransactionResult.GetConsumedResourceTokens();
            result.Success = true;
            result.TransactionFee = GetFeeValue(transactionFees);
            result.ResourceFee = GetFeeValue(resourceFees);
            result.TransactionFees = GetFee(transactionFees);
            result.ResourceFees = GetFee(resourceFees);
        }
        else
        {
            result.Success = false;
            result.Error = TransactionErrorResolver.TakeErrorMessage(
                executionReturnSets.FirstOrDefault()?.TransactionResult.Error, _webAppOptions.IsDebugMode);
        }

        return result;
    }

    private Dictionary<string, long> GetFeeValue(Dictionary<Address, Dictionary<string, long>> feeMap)
    {
        return feeMap?.SelectMany(pair => pair.Value)
            .GroupBy(p => p.Key)
            .ToDictionary(g => g.Key, g => g.Sum(pair => pair.Value));
    }

    private FeeDto GetFee(Dictionary<Address, Dictionary<string, long>> feeMap)
    {
        var fee = feeMap?.Select(f => new FeeDto
        {
            ChargingAddress = f.Key.ToBase58(),
            Fee = f.Value
        }).FirstOrDefault();
        
        return fee;
    }

    private async Task<string[]> PublishTransactionsAsync(string[] rawTransactions)
    {
        var txIds = new string[rawTransactions.Length];
        var transactions = new List<Transaction>();
        for (var i = 0; i < rawTransactions.Length; i++)
        {
            Transaction transaction;
            try
            {
                var byteArray = ByteArrayHelper.HexStringToByteArray(rawTransactions[i]);
                transaction = Transaction.Parser.ParseFrom(byteArray);
            }
            catch
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidTransaction],
                    Error.InvalidTransaction.ToString());
            }

            if (!IsValidMessage(transaction))
                throw new UserFriendlyException(Error.Message[Error.InvalidTransaction],
                    Error.InvalidTransaction.ToString());

            var contractMethodDescriptor =
                await GetContractMethodDescriptorAsync(transaction.To, transaction.MethodName);
            if (contractMethodDescriptor == null)
                throw new UserFriendlyException(Error.Message[Error.NoMatchMethodInContractAddress],
                    Error.NoMatchMethodInContractAddress.ToString());

            var parameters = contractMethodDescriptor.InputType.Parser.ParseFrom(transaction.Params);

            if (!IsValidMessage(parameters))
                throw new UserFriendlyException(Error.Message[Error.InvalidParams], Error.InvalidParams.ToString());

            if (!transaction.VerifySignature())
                throw new UserFriendlyException(Error.Message[Error.InvalidSignature],
                    Error.InvalidSignature.ToString());

            transactions.Add(transaction);
            txIds[i] = transaction.GetHash().ToHex();
        }

        foreach (var transaction in transactions)
            _transactionResultStatusCacheProvider.AddTransactionResultStatus(transaction.GetHash());

        await LocalEventBus.PublishAsync(new TransactionsReceivedEvent
        {
            Transactions = transactions
        });

        return txIds;
    }

    private async Task<MethodDescriptor> GetContractMethodDescriptorAsync(Address contractAddress,
        string methodName, bool throwException = true)
    {
        var chain = await _blockchainService.GetChainAsync();
        var chainContext = new ChainContext
        {
            BlockHash = chain.BestChainHash,
            BlockHeight = chain.BestChainHeight
        };

        return await _transactionReadOnlyExecutionService.GetContractMethodDescriptorAsync(chainContext,
            contractAddress, methodName, throwException);
    }

    private async Task<byte[]> CallReadOnlyAsync(Transaction tx)
    {
        var chainContext = await GetChainContextAsync();

        var trace = await _transactionReadOnlyExecutionService.ExecuteAsync(chainContext, tx,
            DateTime.UtcNow.ToTimestamp());

        if (!string.IsNullOrEmpty(trace.Error))
            throw new Exception(trace.Error);

        return trace.ReturnValue.ToByteArray();
    }

    private async Task<ChainContext> GetChainContextAsync()
    {
        var chain = await _blockchainService.GetChainAsync();
        var chainContext = new ChainContext
        {
            BlockHash = chain.BestChainHash,
            BlockHeight = chain.BestChainHeight
        };
        return chainContext;
    }

    private bool IsValidMessage(IMessage message)
    {
        try
        {
            JsonFormatter.ToDiagnosticString(message);
        }
        catch
        {
            return false;
        }

        return true;
    }
}