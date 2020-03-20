using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Txn.Application;
using AElf.Types;

namespace AElf.Kernel.TransactionPool.Infrastructure
{
    public class TransactionExecutionValidationProvider : ITransactionValidationProvider
    {
        private IPlainTransactionExecutingService _plainTransactionExecutingService;
        private IBlockchainService _blockchainService;

        public TransactionExecutionValidationProvider(
            IPlainTransactionExecutingService plainTransactionExecutingService,
            IBlockchainService blockchainService)
        {
            _plainTransactionExecutingService = plainTransactionExecutingService;
            _blockchainService = blockchainService;
        }
        
        public bool ValidateWhileSyncing { get; } = true;

        public async Task<bool> ValidateTransactionAsync(Transaction transaction)
        {
            var executionReturnSets = await _plainTransactionExecutingService.ExecuteAsync(new TransactionExecutingDto()
            {
                Transactions = new[] {transaction},
                BlockHeader = await _blockchainService.GetBestChainLastBlockHeaderAsync(),
            }, CancellationToken.None);

            return executionReturnSets.FirstOrDefault()?.Status == TransactionResultStatus.Mined;
        }
    }
}