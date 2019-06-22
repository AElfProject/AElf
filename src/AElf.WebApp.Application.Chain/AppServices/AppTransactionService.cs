using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Transactions;
using AElf.WebApp.Application.Chain.AppServices;
using AElf.WebApp.Application.Chain.Dto;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace AElf.WebApp.Application.Chain.AppServices
{
    public interface IAppTransactionService : IApplicationService
    {
        /// <summary>
        /// excute raw transaction
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        Task<string> ExecuteRawTransactionAsync(ExecuteRawTransactionDto input);

        /// <summary>
        /// Call a read-only method on a contract.
        /// </summary>
        /// <returns></returns>
        Task<string> ExecuteTransactionAsync(ExecuteTransactionDto input);

        /// <summary>
        /// Creates an unsigned serialized transaction
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        Task<CreateRawTransactionOutput> CreateRawTransactionAsync(CreateRawTransactionInput input);

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


    /// <summary>
    /// transaction services
    /// </summary>
    /// <seealso cref="Object" />
    /// <seealso cref="AElf.WebApp.Application.Chain.AppServices.IAppTransactionService" />
    public sealed class AppTransactionService : IAppTransactionService
    {
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private readonly ITransactionManager _transactionManager;
        private readonly IAppBlockService _appBlockService;
        private readonly IAppContractService _appContractService;


        /// <summary>
        /// Initializes a new instance of the <see cref="AppTransactionService"/> class.
        /// </summary>
        /// <param name="transactionReadOnlyExecutionService">The transaction read only execution service.</param>
        /// <param name="transactionManager">The transaction manager.</param>
        /// <param name="txHub">The tx hub.</param>
        /// <param name="appBlockService">The application block service.</param>
        /// <param name="appContractService">The application contract service.</param>
        public AppTransactionService(
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
            ITransactionManager transactionManager,
            IAppBlockService appBlockService,
            IAppContractService appContractService)
        {
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
            _transactionManager = transactionManager;
            _appBlockService = appBlockService;
            _appContractService = appContractService;
        }

        #region execute transaction

        /// <summary>
        /// excute raw transaction
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<string> ExecuteRawTransactionAsync(ExecuteRawTransactionDto input)
        {
            try
            {
                var byteArray = ByteArrayHelpers.FromHexString(input.RawTransaction);
                var transaction = Transaction.Parser.ParseFrom(byteArray);
                transaction.Signature = ByteString.CopyFrom(ByteArrayHelpers.FromHexString(input.Signature));
                if (!transaction.VerifySignature())
                {
                    throw new UserFriendlyException(Error.Message[Error.InvalidTransaction],
                        Error.InvalidTransaction.ToString());
                }

                var response = await CallReadOnlyReturnReadableValueAsync(transaction);
                return response;
            }
            catch
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidTransaction],
                    Error.InvalidTransaction.ToString());
            }
        }

        /// <summary>
        /// Call a read-only method on a contract.
        /// </summary>
        /// <returns></returns>
        public async Task<string> ExecuteTransactionAsync(ExecuteTransactionDto input)
        {
            try
            {
                var byteArray = ByteArrayHelpers.FromHexString(input.RawTransaction);
                var transaction = Transaction.Parser.ParseFrom(byteArray);
                if (!transaction.VerifySignature())
                {
                    throw new UserFriendlyException(Error.Message[Error.InvalidTransaction],
                        Error.InvalidTransaction.ToString());
                }

                var response = await CallReadOnlyAsync(transaction);
                return response?.ToHex();
            }
            catch
            {
                throw new UserFriendlyException(Error.Message[Error.InvalidTransaction],
                    Error.InvalidTransaction.ToString());
            }
        }

        #endregion execute transaction

        #region create transaction

        /// <summary>
        /// Creates an unsigned serialized transaction
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<CreateRawTransactionOutput> CreateRawTransactionAsync(CreateRawTransactionInput input)
        {
            var transaction = new Transaction
            {
                From = Address.Parse(input.From),
                To = Address.Parse(input.To),
                RefBlockNumber = input.RefBlockNumber,
                RefBlockPrefix = ByteString.CopyFrom(Hash.LoadHex(input.RefBlockHash).Value.Take(4).ToArray()),
                MethodName = input.MethodName
            };

            var methodDescriptor =
                await _appContractService.GetContractMethodDescriptorAsync(Address.Parse(input.To), input.MethodName);
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

        #endregion create transaction

        #region get transaction

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

            var transactionResult = await GetTransactionResultAsync(transactionHash);
            var transaction = await _transactionManager.GetTransaction(transactionResult.TransactionId);

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
                await _appContractService.GetContractMethodDescriptorAsync(transaction.To, transaction.MethodName);
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
                        await _appContractService.GetContractMethodDescriptorAsync(transaction.To,
                            transaction.MethodName);
                    transactionResultDto.Transaction.Params = JsonFormatter.ToDiagnosticString(
                        methodDescriptor.InputType.Parser.ParseFrom(transaction.Params));

                    transactionResultDto.Status = transactionResult.Status.ToString();
                    output.Add(transactionResultDto);
                }
            }

            return output;
        }

        #endregion get transaction


        /// <summary>
        /// Calls the read only asynchronous.
        /// </summary>
        /// <param name="tx">The tx.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// 
        private async Task<byte[]> CallReadOnlyAsync(Transaction tx)
        {
            var chainContext = await _appBlockService.GetChainContextAsync();

            var trace = await _transactionReadOnlyExecutionService.ExecuteAsync(chainContext, tx,
                DateTime.UtcNow.ToTimestamp());

            if (!string.IsNullOrEmpty(trace.StdErr))
                throw new Exception(trace.StdErr);

            return trace.ReturnValue.ToByteArray();
        }

        /// <summary>
        /// Calls the read only return readable value asynchronous.
        /// </summary>
        /// <param name="tx">The tx.</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task<string> CallReadOnlyReturnReadableValueAsync(Transaction tx)
        {
            var chainContext = await _appBlockService.GetChainContextAsync();

            var trace = await _transactionReadOnlyExecutionService.ExecuteAsync(chainContext, tx,
                DateTime.UtcNow.ToTimestamp());

            if (!string.IsNullOrEmpty(trace.StdErr))
                throw new Exception(trace.StdErr);

            return trace.ReadableReturnValue;
        }

        #region tool

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

        #endregion tool
    }
}