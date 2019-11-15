using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.TransactionPool.Application
{
    internal class TransactionToAddressValidationProvider : ITransactionValidationProvider
    {
        private readonly IDeployedContractAddressProvider _deployedContractAddressProvider;

        public ILogger<TransactionToAddressValidationProvider> Logger { get; set; }

        public TransactionToAddressValidationProvider(IDeployedContractAddressProvider deployedContractAddressProvider)
        {
            _deployedContractAddressProvider = deployedContractAddressProvider;
        }

        public Task<bool> ValidateTransactionAsync(Transaction transaction)
        {
            if (_deployedContractAddressProvider.CheckContractAddress(transaction.To))
            {
                return Task.FromResult(true);
            }

            Logger.LogError($"Invalid contract address: {transaction}");
            return Task.FromResult(false);
        }
    }
}