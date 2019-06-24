using AElf.Types;
using AElf.WebApp.Application.Chain.Dto;
using Google.Protobuf;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.DependencyInjection;

namespace AElf.WebApp.Application.Chain.AppServices.AppTransactionResultService
{
    public interface IAppTransactionResultService
    {
        /// <summary>
        /// Get the current status of a transaction
        /// </summary>
        /// <param name="transactionId">transaction id</param>
        /// <returns></returns>
        Task<TransactionResultDto> GetTransactionResultAsync(string transactionId);

        /// <summary>
        /// Get multiple transaction results.
        /// </summary>
        /// <param name="blockHash">block hash</param>
        /// <param name="offset">offset</param>
        /// <param name="limit">limit</param>
        /// <returns></returns>
        /// <exception cref="UserFriendlyException"></exception>
        Task<List<TransactionResultDto>> GetTransactionResultsAsync(string blockHash, int offset = 0,
            int limit = 10);
    }

    public class AppTransactionResultService : IAppTransactionResultService,ITransientDependency
    {
        private readonly IAppBlockService _appBlockService;
        private readonly IAppTransactionGetResultService _appTransactionGetResultService;
        private readonly IAppTransactionService _appTransactionService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AppTransactionResultService"/> class.
        /// </summary>
        /// <param name="appBlockService">The application block service.</param>
        /// <param name="appTransactionService">The application contract service.</param>
        /// <param name="appTransactionGetResultService">The application transaction get result service.</param>
        public AppTransactionResultService(IAppBlockService appBlockService,
            IAppTransactionService appTransactionService,
            IAppTransactionGetResultService appTransactionGetResultService)
        {
            _appBlockService = appBlockService;
            _appTransactionService = appTransactionService;
            _appTransactionGetResultService = appTransactionGetResultService;
        }

        /// <summary>
        /// Get the current status of a transaction
        /// </summary>
        /// <param name="transactionId">transaction id</param>
        /// <returns></returns>
        public async Task<TransactionResultDto> GetTransactionResultAsync(string transactionId)
        {
            Hash transactionHash;
            try
            {
                transactionHash = Hash.LoadHex(transactionId);
            }
            catch
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidTransactionId],
                    Error.InvalidTransactionId.ToString());
            }

            var transactionAndResult =
                await _appTransactionGetResultService.GetTransactionAndResultAsync(transactionHash);

            var transactionResult = transactionAndResult.Item1;
            var transaction = transactionAndResult.Item2;

            var output = JsonConvert.DeserializeObject<TransactionResultDto>(transactionResult.ToString());
            if (transactionResult.Status == TransactionResultStatus.Mined)
            {
                var block = await _appBlockService.GetBlockAtHeightAsync(transactionResult.BlockNumber);
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

            var methodDescriptor =
                await _appTransactionService.GetContractMethodDescriptorAsync(transaction.To, transaction.MethodName);
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
                realBlockHash = Hash.LoadHex(blockHash);
            }
            catch
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidBlockHash],
                    Error.InvalidBlockHash.ToString());
            }

            var block = await _appBlockService.GetBlockAsync(realBlockHash);
            if (block == null)
            {
                throw new UserFriendlyException(Error.Message[Error.NotFound], Error.NotFound.ToString());
            }

            var output = new List<TransactionResultDto>();
            if (offset <= block.Body.Transactions.Count - 1)
            {
                limit = Math.Min(limit, block.Body.Transactions.Count - offset);
                var transactionHashes = block.Body.Transactions.ToList().GetRange(offset, limit);
                foreach (var hash in transactionHashes)
                {
                    var transactionAndResult = await _appTransactionGetResultService.GetTransactionAndResultAsync(hash);

                    var transactionResult = transactionAndResult.Item1;
                    var transaction = transactionAndResult.Item2;

                    var transactionResultDto =
                        JsonConvert.DeserializeObject<TransactionResultDto>(transactionResult.ToString());

                    transactionResultDto.BlockHash = block.GetHash().ToHex();

                    if (transactionResult.Status == TransactionResultStatus.Failed)
                        transactionResultDto.Error = transactionResult.Error;

                    transactionResultDto.Transaction =
                        JsonConvert.DeserializeObject<TransactionDto>(transaction.ToString());

                    var methodDescriptor =
                        await _appTransactionService.GetContractMethodDescriptorAsync(transaction.To,
                            transaction.MethodName);
                    transactionResultDto.Transaction.Params = JsonFormatter.ToDiagnosticString(
                        methodDescriptor.InputType.Parser.ParseFrom(transaction.Params));

                    transactionResultDto.Status = transactionResult.Status.ToString();
                    output.Add(transactionResultDto);
                }
            }

            return output;
        }
    }
}