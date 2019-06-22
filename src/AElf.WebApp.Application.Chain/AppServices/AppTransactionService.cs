using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Threading.Tasks;
using System.Transactions;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Types;
using AElf.WebApp.Application.Chain.Dto;
using Google.Protobuf;
using Google.Protobuf.Reflection;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.EventBus.Local;

using Hash = AElf.Types.Hash;
using Transaction=AElf.Types.Transaction;

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
    }


    /// <summary>
    /// transaction services
    /// </summary>
    /// <seealso cref="Object" />
    /// <seealso cref="AElf.WebApp.Application.Chain.AppServices.IAppTransactionService" />
    public sealed class AppTransactionService : IAppTransactionService
    {
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;
        private readonly IAppBlockService _appBlockService;
        private readonly IAppContractService _appContractService;


        /// <summary>
        /// Initializes a new instance of the <see cref="AppTransactionService"/> class.
        /// </summary>
        /// <param name="transactionReadOnlyExecutionService">The transaction read only execution service.</param>
        /// <param name="appBlockService">The application block service.</param>
        /// <param name="appContractService">The application contract service.</param>
        public AppTransactionService(
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService,
            IAppBlockService appBlockService,
            IAppContractService appContractService)
        {
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
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