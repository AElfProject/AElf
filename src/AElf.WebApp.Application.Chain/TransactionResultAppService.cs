using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using AElf.WebApp.Application.Chain.Dto;
using Google.Protobuf;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace AElf.WebApp.Application.Chain
{
    public interface ITransactionResultAppService : IApplicationService
    {
        Task<TransactionResultDto> GetTransactionResultAsync(string transactionId);

        Task<List<TransactionResultDto>> GetTransactionResultsAsync(string blockHash, int offset = 0,
            int limit = 10);
    }

    [ControllerName("BlockChain")]
    public class TransactionResultAppService : ITransactionResultAppService
    {
        private readonly ITransactionResultProxyService _transactionResultProxyService;
        private readonly ITransactionManager _transactionManager;
        private readonly IBlockchainService _blockchainService;
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;

        public TransactionResultAppService(ITransactionResultProxyService transactionResultProxyService,
            ITransactionManager transactionManager,
            IBlockchainService blockchainService,
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService)
        {
            _transactionResultProxyService = transactionResultProxyService;
            _transactionManager = transactionManager;
            _blockchainService = blockchainService;
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
        }

        /// <summary>
        /// Get the current status of a transaction
        /// </summary>
        /// <param name="transactionId">transaction id</param>
        /// <returns></returns>
        public async Task<TransactionResultDto> GetTransactionResultAsync(string transactionId)
        {
            Hash transactionIdHash;
            try
            {
                transactionIdHash = HashHelper.HexStringToHash(transactionId);
            }
            catch
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidTransactionId],
                    Error.InvalidTransactionId.ToString());
            }

            var transactionResult = await GetTransactionResultAsync(transactionIdHash);
            var transaction = await _transactionManager.GetTransaction(transactionResult.TransactionId);

            var output = JsonConvert.DeserializeObject<TransactionResultDto>(transactionResult.ToString());
            if (transactionResult.Status == TransactionResultStatus.Mined)
            {
                var block = await _blockchainService.GetBlockAtHeightAsync(transactionResult.BlockNumber);
                output.BlockHash = block.GetHash().ToHex();
            }

            if (transactionResult.Status == TransactionResultStatus.Failed)
                output.Error = transactionResult.Error;

            if (transactionResult.Status == TransactionResultStatus.NotExisted)
            {
                output.Status = transactionResult.Status.ToString();
                return output;
            }

            output.Transaction = JsonConvert.DeserializeObject<TransactionDto>(transaction.ToString());

            var methodDescriptor = await ContractMethodDescriptorHelper.GetContractMethodDescriptorAsync(
                _blockchainService, _transactionReadOnlyExecutionService, transaction.To, transaction.MethodName);

            output.Transaction.Params = JsonFormatter.ToDiagnosticString(
                methodDescriptor.InputType.Parser.ParseFrom(transaction.Params));

            return output;
        }

        /// <summary>
        /// Get multiple transaction results.
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
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidOffset], Error.InvalidOffset.ToString());
            }

            if (limit <= 0 || limit > 100)
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidLimit], Error.InvalidLimit.ToString());
            }

            Hash realBlockHash;
            try
            {
                realBlockHash = HashHelper.HexStringToHash(blockHash);
            }
            catch
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidBlockHash],
                    Error.InvalidBlockHash.ToString());
            }

            var block = await _blockchainService.GetBlockAsync(realBlockHash);
            if (block == null)
            {
                throw new UserFriendlyException(Error.Message[Error.NotFound], Error.NotFound.ToString());
            }

            var output = new List<TransactionResultDto>();
            if (offset <= block.Body.TransactionIds.Count - 1)
            {
                limit = Math.Min(limit, block.Body.TransactionIds.Count - offset);
                var transactionIds = block.Body.TransactionIds.ToList().GetRange(offset, limit);
                foreach (var hash in transactionIds)
                {
                    var transactionResult = await GetTransactionResultAsync(hash);
                    var transactionResultDto =
                        JsonConvert.DeserializeObject<TransactionResultDto>(transactionResult.ToString());
                    var transaction = await _transactionManager.GetTransaction(transactionResult.TransactionId);
                    transactionResultDto.BlockHash = block.GetHash().ToHex();

                    if (transactionResult.Status == TransactionResultStatus.Failed)
                        transactionResultDto.Error = transactionResult.Error;

                    transactionResultDto.Transaction =
                        JsonConvert.DeserializeObject<TransactionDto>(transaction.ToString());

                    var methodDescriptor =
                        await ContractMethodDescriptorHelper.GetContractMethodDescriptorAsync(_blockchainService,
                            _transactionReadOnlyExecutionService, transaction.To, transaction.MethodName);

                    transactionResultDto.Transaction.Params = JsonFormatter.ToDiagnosticString(
                        methodDescriptor.InputType.Parser.ParseFrom(transaction.Params));

                    transactionResultDto.Status = transactionResult.Status.ToString();
                    output.Add(transactionResultDto);
                }
            }

            return output;
        }

        private async Task<TransactionResult> GetTransactionResultAsync(Hash transactionId)
        {
            // in tx pool
            var receipt = await _transactionResultProxyService.TxHub.GetTransactionReceiptAsync(transactionId);
            if (receipt != null)
            {
                return new TransactionResult
                {
                    TransactionId = receipt.TransactionId,
                    Status = TransactionResultStatus.Pending
                };
            }
            
            // in storage
            var res = await _transactionResultProxyService.TransactionResultQueryService.GetTransactionResultAsync(transactionId);
            if (res != null)
            {
                return res;
            }

            // not existed
            return new TransactionResult
            {
                TransactionId = transactionId,
                Status = TransactionResultStatus.NotExisted
            };
        }
    }
}