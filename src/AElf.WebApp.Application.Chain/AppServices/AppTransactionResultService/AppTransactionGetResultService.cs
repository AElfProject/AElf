using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.TransactionPool.Infrastructure;
using AElf.Types;
using AElf.WebApp.Application.Chain.Dto;
using Microsoft.AspNetCore.Routing.Template;
using Newtonsoft.Json;
using Volo.Abp.Application.Services;

namespace AElf.WebApp.Application.Chain.AppServices.AppTransactionResultService
{
    public interface IAppTransactionGetResultService : IApplicationService
    {
        /// <summary>
        /// Gets the transaction asynchronous.
        /// </summary>
        /// <param name="hash">The hash.</param>
        /// <returns></returns>
        Task<Transaction> GetTransactionAsync(Hash hash);

        /// <summary>
        /// Gets the transaction and result asynchronous.
        /// </summary>
        /// <param name="hash">The hash.</param>
        /// <returns></returns>
        Task<(TransactionResult, Transaction)> GetTransactionAndResultAsync(Hash hash);
    }

    public sealed class AppTransactionGetResultService : IAppTransactionGetResultService
    {
        private readonly ITransactionResultQueryService _transactionResultQueryService;

        private readonly ITxHub _txHub;

        private readonly ITransactionManager _transactionManager;

        public AppTransactionGetResultService(ITransactionManager transactionManager,
            ITxHub txHub,
            ITransactionResultQueryService transactionResultQueryService)
        {
            _transactionResultQueryService = transactionResultQueryService;
            _txHub = txHub;
            _transactionManager = transactionManager;
        }

        /// <summary>
        /// Gets the transaction asynchronous.
        /// </summary>
        /// <param name="hash">The hash.</param>
        /// <returns></returns>
        public async Task<Transaction> GetTransactionAsync(Hash hash)
        {
            var transactionResult = await GetTransactionResultAsync(hash);
            return await _transactionManager.GetTransaction(transactionResult.TransactionId);
        }

        /// <summary>
        /// Gets the transaction asynchronous.
        /// </summary>
        /// <param name="hash">The hash.</param>
        /// <returns>
        /// item1 is TransactionResultDto,item2 is Transaction
        /// </returns>
        public async Task<(TransactionResult, Transaction)> GetTransactionAndResultAsync(Hash hash)
        {
            var transactionResult = await GetTransactionResultAsync(hash);
            var transacrion = await _transactionManager.GetTransaction(transactionResult.TransactionId);
            return (transactionResult, transacrion);
        }


        /// <summary>
        /// Gets the transaction result asynchronous.
        /// </summary>
        /// <param name="txHash">The tx hash.</param>
        /// <returns></returns>
        private async Task<TransactionResult> GetTransactionResultAsync(Hash txHash)
        {
            // in storage
            var res = await _transactionResultQueryService.GetTransactionResultAsync(txHash);
            if (res != null)
            {
                return res;
            }

            // in tx pool
            var receipt = await _txHub.GetTransactionReceiptAsync(txHash);
            if (receipt != null)
            {
                return new TransactionResult
                {
                    TransactionId = receipt.TransactionId,
                    Status = TransactionResultStatus.Pending
                };
            }

            // not existed
            return new TransactionResult
            {
                TransactionId = txHash,
                Status = TransactionResultStatus.NotExisted
            };
        }
    }
}