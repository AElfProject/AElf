using System.Threading.Tasks;
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

        public async Task<bool> ValidateTransactionAsync(Transaction transaction)
        {
            var deployedContractAddressList = await _deployedContractAddressProvider.GetDeployedContractAddressListAsync();
            if (deployedContractAddressList.Value.Contains(transaction.To))
            {
                return true;
            }

            Logger.LogError($"Invalid contract address: {transaction}");
            return false;
        }
    }
}