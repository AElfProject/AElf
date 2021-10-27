using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Txn.Application;
using AElf.Types;
using Microsoft.Extensions.Options;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.TransactionPool.Infrastructure
{
    public class TransactionExecutionValidationProvider : ITransactionValidationProvider
    {
        private readonly IPlainTransactionExecutingService _plainTransactionExecutingService;
        private readonly TransactionOptions _transactionOptions;
        public ILocalEventBus LocalEventBus { get; set; }

        public TransactionExecutionValidationProvider(
            IPlainTransactionExecutingService plainTransactionExecutingService,
            IBlockchainService blockchainService, IOptionsMonitor<TransactionOptions> transactionOptionsMonitor)
        {
            _plainTransactionExecutingService = plainTransactionExecutingService;
            _transactionOptions = transactionOptionsMonitor.CurrentValue;
            LocalEventBus = NullLocalEventBus.Instance;
        }

        public bool ValidateWhileSyncing { get; } = false;

        public async Task<bool> ValidateTransactionAsync(Transaction transaction, IChainContext chainContext)
        {
            if (!_transactionOptions.EnableTransactionExecutionValidation)
                return true;

            var executionReturnSets = await _plainTransactionExecutingService.ExecuteAsync(new TransactionExecutingDto()
            {
                Transactions = new[] {transaction},
                BlockHeader = new BlockHeader
                {
                    PreviousBlockHash = chainContext.BlockHash,
                    Height = chainContext.BlockHeight + 1,
                    Time = TimestampHelper.GetUtcNow(),
                }
            }, CancellationToken.None);

            var executionValidationResult =
                executionReturnSets.FirstOrDefault()?.Status == TransactionResultStatus.Mined;
            if (!executionValidationResult)
            {
                var transactionId = transaction.GetHash();
                // TODO: Consider to remove TransactionExecutionValidationFailedEvent.
                await LocalEventBus.PublishAsync(new TransactionExecutionValidationFailedEvent
                {
                    TransactionId = transactionId
                });
                await LocalEventBus.PublishAsync(new TransactionValidationStatusChangedEvent
                {
                    TransactionId = transactionId,
                    TransactionResultStatus = TransactionResultStatus.NodeValidationFailed,
                    Error = executionReturnSets.FirstOrDefault()?.TransactionResult?.Error ?? string.Empty
                });
            }

            return executionValidationResult;
        }
    }
}