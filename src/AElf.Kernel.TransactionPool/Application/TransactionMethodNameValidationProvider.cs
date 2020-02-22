using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Kernel.Txn.Application;
using AElf.Types;

namespace AElf.Kernel.TransactionPool.Application
{
    /// <summary>
    /// We need to prevent some txs from adding them to either tx hub or block bodies.
    /// </summary>
    internal class TransactionMethodNameValidationProvider : ITransactionValidationProvider
    {
        public bool ValidateWhileSyncing => true;

        private readonly ISmartContractAddressService _smartContractAddressService;

        public TransactionMethodNameValidationProvider(ISmartContractAddressService smartContractAddressService)
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

            //TODO: should not know token
            var txsGeneratedByPlugins = new List<string>
            {
                nameof(TokenContractContainer.TokenContractStub.ChargeTransactionFees),
                nameof(TokenContractContainer.TokenContractStub.ChargeResourceToken),
                nameof(TokenContractContainer.TokenContractStub.CheckThreshold),
                nameof(TokenContractContainer.TokenContractStub.CheckResourceToken)
            };
            return Task.FromResult(!txsGeneratedByPlugins.Contains(transaction.MethodName));
        }
    }
}