using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using AElf.WebApp.Application.Chain.Dto;
using AElf.WebApp.Application.Chain.Infrastructure;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.ObjectMapping;

namespace AElf.WebApp.Application.Chain;

public interface ITransactionResultAppService
{
    Task<TransactionResultDto> GetTransactionResultAsync(string transactionId);

    Task<List<TransactionResultDto>> GetTransactionResultsAsync(string blockHash, int offset = 0,
        int limit = 10);

    Task<MerklePathDto> GetMerklePathByTransactionIdAsync(string transactionId);
}

public class TransactionResultAppService : AElfAppService, ITransactionResultAppService
{
    private readonly IBlockchainService _blockchainService;
    private readonly IObjectMapper<ChainApplicationWebAppAElfModule> _objectMapper;
    private readonly ITransactionManager _transactionManager;
    private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
    private readonly ITransactionResultProxyService _transactionResultProxyService;
    private readonly ITransactionResultStatusCacheProvider _transactionResultStatusCacheProvider;
    private readonly WebAppOptions _webAppOptions;

    public TransactionResultAppService(ITransactionResultProxyService transactionResultProxyService,
        ITransactionManager transactionManager,
        IBlockchainService blockchainService,
        ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
        IObjectMapper<ChainApplicationWebAppAElfModule> objectMapper,
        ITransactionResultStatusCacheProvider transactionResultStatusCacheProvider,
        IOptionsMonitor<WebAppOptions> optionsSnapshot)
    {
        _transactionResultProxyService = transactionResultProxyService;
        _transactionManager = transactionManager;
        _blockchainService = blockchainService;
        _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
        _objectMapper = objectMapper;
        _transactionResultStatusCacheProvider = transactionResultStatusCacheProvider;
        _webAppOptions = optionsSnapshot.CurrentValue;

        Logger = NullLogger<TransactionResultAppService>.Instance;
    }

    public ILogger<TransactionResultAppService> Logger { get; set; }

    /// <summary>
    ///     Get the current status of a transaction
    /// </summary>
    /// <param name="transactionId">transaction id</param>
    /// <returns></returns>
    public async Task<TransactionResultDto> GetTransactionResultAsync(string transactionId)
    {
        Hash transactionIdHash;
        try
        {
            transactionIdHash = Hash.LoadFromHex(transactionId);
        }
        catch
        {
            throw new UserFriendlyException(Error.Message[Error.InvalidTransactionId],
                Error.InvalidTransactionId.ToString());
        }

        var transactionResult = await GetTransactionResultAsync(transactionIdHash);
        var output = _objectMapper.GetMapper()
            .Map<TransactionResult, TransactionResultDto>(transactionResult,
                opt => opt.Items[TransactionProfile.ErrorTrace] = _webAppOptions.IsDebugMode);

        var transaction = await _transactionManager.GetTransactionAsync(transactionResult.TransactionId);
        output.Transaction = _objectMapper.Map<Transaction, TransactionDto>(transaction);
        output.TransactionSize = transaction?.CalculateSize() ?? 0;

        if (transactionResult.Status == TransactionResultStatus.NotExisted)
        {
            var validationStatus =
                _transactionResultStatusCacheProvider.GetTransactionResultStatus(transactionIdHash);
            if (validationStatus != null)
            {
                output.Status = validationStatus.TransactionResultStatus.ToString().ToUpper();
                output.Error =
                    TransactionErrorResolver.TakeErrorMessage(validationStatus.Error, _webAppOptions.IsDebugMode);
            }

            return output;
        }
        
        await FormatTransactionParamsAsync(output.Transaction, transaction.Params);
        
        return output;
    }

    /// <summary>
    ///     Get multiple transaction results.
    /// </summary>
    /// <param name="blockHash">block hash</param>
    /// <param name="offset">offset</param>
    /// <param name="limit">limit</param>
    /// <returns></returns>
    /// <exception cref="UserFriendlyException"></exception>
    public async Task<List<TransactionResultDto>> GetTransactionResultsAsync(string blockHash, int offset = 0,
        int limit = 10)
    {
        if (offset < 0)
            throw new UserFriendlyException(Error.Message[Error.InvalidOffset], Error.InvalidOffset.ToString());

        if (limit <= 0 || limit > 100)
            throw new UserFriendlyException(Error.Message[Error.InvalidLimit], Error.InvalidLimit.ToString());

        Hash realBlockHash;
        try
        {
            realBlockHash = Hash.LoadFromHex(blockHash);
        }
        catch
        {
            throw new UserFriendlyException(Error.Message[Error.InvalidBlockHash],
                Error.InvalidBlockHash.ToString());
        }

        var block = await _blockchainService.GetBlockAsync(realBlockHash);
        if (block == null) throw new UserFriendlyException(Error.Message[Error.NotFound], Error.NotFound.ToString());

        var output = new List<TransactionResultDto>();
        if (offset <= block.Body.TransactionIds.Count - 1)
        {
            limit = Math.Min(limit, block.Body.TransactionIds.Count - offset);
            var transactionIds = block.Body.TransactionIds.ToList().GetRange(offset, limit);
            foreach (var transactionId in transactionIds)
            {
                var transactionResultDto = await GetTransactionResultDto(transactionId, realBlockHash, block.GetHash());
                output.Add(transactionResultDto);
            }
        }

        return output;
    }

    /// <summary>
    ///     Get the merkle path of a transaction.
    /// </summary>
    /// <param name="transactionId"></param>
    /// <returns></returns>
    public async Task<MerklePathDto> GetMerklePathByTransactionIdAsync(string transactionId)
    {
        Hash transactionIdHash;
        try
        {
            transactionIdHash = Hash.LoadFromHex(transactionId);
        }
        catch
        {
            throw new UserFriendlyException(Error.Message[Error.InvalidTransactionId],
                Error.InvalidTransactionId.ToString());
        }

        var transactionResult = await GetMinedTransactionResultAsync(transactionIdHash);
        var blockHash = transactionResult.BlockHash;
        var blockInfo = await _blockchainService.GetBlockByHashAsync(blockHash);
        var transactionIds = blockInfo.Body.TransactionIds;
        var index = transactionIds.IndexOf(transactionIdHash);
        if (index == -1) throw new UserFriendlyException(Error.Message[Error.NotFound], Error.NotFound.ToString());
        var leafNodes = await GetLeafNodesAsync(blockInfo.TransactionIds);

        var binaryMerkleTree = BinaryMerkleTree.FromLeafNodes(leafNodes);
        var path = binaryMerkleTree.GenerateMerklePath(index);
        var merklePath = _objectMapper.Map<MerklePath, MerklePathDto>(path);

        return merklePath;
    }

    private async Task<TransactionResult> GetTransactionResultAsync(Hash transactionId, Hash blockHash = null)
    {
        // in tx pool
        var queuedTransaction =
            await _transactionResultProxyService.TransactionPoolService.GetQueuedTransactionAsync(transactionId);
        if (queuedTransaction != null)
            return new TransactionResult
            {
                TransactionId = queuedTransaction.TransactionId,
                Status = TransactionResultStatus.Pending
            };

        // in storage
        TransactionResult result;
        if (blockHash != null)
            result =
                await _transactionResultProxyService.TransactionResultQueryService.GetTransactionResultAsync(
                    transactionId, blockHash);
        else
            result =
                await _transactionResultProxyService.TransactionResultQueryService.GetTransactionResultAsync(
                    transactionId);

        if (result != null) return result;

        // not existed
        return new TransactionResult
        {
            TransactionId = transactionId,
            Status = TransactionResultStatus.NotExisted
        };
    }

    private async Task<TransactionResultDto> GetTransactionResultDto(Hash transactionId, Hash realBlockHash,
        Hash blockHash)
    {
        var transactionResult = await GetTransactionResultAsync(transactionId, realBlockHash);
        var transactionResultDto = _objectMapper.GetMapper()
            .Map<TransactionResult, TransactionResultDto>(transactionResult,
                opt => opt.Items[TransactionProfile.ErrorTrace] = _webAppOptions.IsDebugMode);


        var transaction = await _transactionManager.GetTransactionAsync(transactionResult.TransactionId);
        transactionResultDto.BlockHash = blockHash.ToHex();

        transactionResultDto.Transaction = _objectMapper.Map<Transaction, TransactionDto>(transaction);
        transactionResultDto.TransactionSize = transaction.CalculateSize();

        await FormatTransactionParamsAsync(transactionResultDto.Transaction, transaction.Params);

        return transactionResultDto;
    }

    private async Task<TransactionResult> GetMinedTransactionResultAsync(Hash transactionIdHash)
    {
        var transactionResult = await GetTransactionResultAsync(transactionIdHash);
        switch (transactionResult.Status)
        {
            case TransactionResultStatus.Mined:
            {
                var block = await _blockchainService.GetBlockAtHeightAsync(transactionResult.BlockNumber);
                transactionResult.BlockHash = block.GetHash();
                break;
            }
            case TransactionResultStatus.Failed:
            case TransactionResultStatus.NotExisted:
                throw new UserFriendlyException(Error.Message[Error.NotFound], Error.NotFound.ToString());
        }

        return transactionResult;
    }

    private async Task<List<Hash>> GetLeafNodesAsync(IEnumerable<Hash> transactionIds)
    {
        var transactionResultList = new List<TransactionResult>();
        foreach (var item in transactionIds)
        {
            var result = await GetTransactionResultAsync(item);
            transactionResultList.Add(result);
        }

        var transactionResultSet = transactionResultList.Select(txResult => (txResult.TransactionId, txResult.Status));
        var leafNodes = new List<Hash>();
        foreach (var (txId, status) in transactionResultSet)
            leafNodes.Add(GetHashCombiningTransactionAndStatus(txId, status));

        return leafNodes;
    }

    private Hash GetHashCombiningTransactionAndStatus(Hash txId,
        TransactionResultStatus executionReturnStatus)
    {
        // combine tx result status
        var rawBytes = txId.ToByteArray().Concat(Encoding.UTF8.GetBytes(executionReturnStatus.ToString()))
            .ToArray();
        return HashHelper.ComputeFrom(rawBytes);
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

    private async Task FormatTransactionParamsAsync(TransactionDto transaction, ByteString @params)
    {
        var methodDescriptor =
            await GetContractMethodDescriptorAsync(Address.FromBase58(transaction.To), transaction.MethodName, false);

        if (methodDescriptor == null)
            return;

        try
        {
            var parameters = methodDescriptor.InputType.Parser.ParseFrom(@params);
            transaction.Params = JsonFormatter.ToDiagnosticString(parameters);;
        }
        catch (Exception exception)
        {
            Logger.LogError(exception, "Failed to parse transaction params: {params}", transaction.Params);
        }
    }
}