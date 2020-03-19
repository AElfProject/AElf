using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Kernel.Txn.Application;
using AElf.Types;

namespace AElf.Kernel.TransactionPool.Application
{
    internal class TxHubEntryPermissionValidationProvider : ITransactionValidationProvider
    {
        public bool ValidateWhileSyncing => false;

        private readonly ISmartContractAddressService _smartContractAddressService;

        public TxHubEntryPermissionValidationProvider(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public Task<bool> ValidateTransactionAsync(Transaction transaction)
        {
            var tokenContractAddress =
                _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name);
            if (transaction.To != tokenContractAddress)
            {
                return Task.FromResult(true);
            }

            var systemTxs = new List<string>
            {
                nameof(TokenContractContainer.TokenContractStub.ClaimTransactionFees),
                nameof(TokenContractContainer.TokenContractStub.DonateResourceToken),
            };
            return Task.FromResult(!systemTxs.Contains(transaction.MethodName));
        }
    }
}