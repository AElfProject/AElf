using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Txn.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.TransactionPool.Application
{
    public class TransactionValidationService : ITransactionValidationService, ITransientDependency
    {
        private readonly IEnumerable<ITransactionValidationProvider> _transactionValidationProviders;
        private readonly IBlockchainService _blockchainService;

        public ILogger<TransactionValidationService> Logger { get; set; }

        public TransactionValidationService(
            IEnumerable<ITransactionValidationProvider> transactionValidationProviders, 
            IBlockchainService blockchainService)
        {
            _transactionValidationProviders = transactionValidationProviders;
            _blockchainService = blockchainService;

            Logger = NullLogger<TransactionValidationService>.Instance;
        }

        /// <summary>
        /// Validate txs before they enter tx hub.
        /// </summary>
        /// <param name="chainContext"></param>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public async Task<bool> ValidateTransactionWhileCollectingAsync(IChainContext chainContext,
            Transaction transaction)
        {
            foreach (var provider in _transactionValidationProviders)
            {
                if (await provider.ValidateTransactionAsync(transaction, chainContext)) continue;
                Logger.LogWarning(
                    $"[ValidateTransactionWhileCollectingAsync]Transaction {transaction.GetHash()} validation failed in {provider.GetType()}");
                return false;
            }

            return true;
        }

        public async Task<bool> ValidateTransactionWhileSyncingAsync(Transaction transaction)
        {
            foreach (var provider in _transactionValidationProviders)
            {
                if (!provider.ValidateWhileSyncing ||
                    await provider.ValidateTransactionAsync(transaction)) continue;
                Logger.LogWarning(
                    $"[ValidateTransactionWhileSyncingAsync]Transaction {transaction.GetHash()} validation failed in {provider.GetType()}");
                return false;
            }

            return true;
        }
    }
}