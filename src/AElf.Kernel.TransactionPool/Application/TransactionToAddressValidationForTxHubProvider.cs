using System.Threading.Tasks;
using AElf.Types;

namespace AElf.Kernel.TransactionPool.Application
{
    internal class TransactionToAddressValidationForTxHubProvider : ITransactionValidationForTxHubProvider
    {
        private readonly IDeployedContractAddressProvider _deployedContractAddressProvider;

        public TransactionToAddressValidationForTxHubProvider(IDeployedContractAddressProvider deployedContractAddressProvider)
        {
            _deployedContractAddressProvider = deployedContractAddressProvider;
        }

        public async Task<bool> ValidateTransactionAsync(Transaction transaction)
        {
            var deployedContractAddressList = await _deployedContractAddressProvider.GetDeployedContractAddressListAsync();
            return deployedContractAddressList.Value.Contains(transaction.To);
        }
    }
}