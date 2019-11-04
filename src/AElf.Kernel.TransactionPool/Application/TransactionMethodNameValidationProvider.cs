using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.Token;
using AElf.Types;

namespace AElf.Kernel.TransactionPool.Application
{
    /// <summary>
    /// We need to prevent some txs from adding them to either tx hub or block bodies.
    /// </summary>
    internal class TransactionMethodNameValidationProvider : ITransactionValidationProvider
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        public TransactionMethodNameValidationProvider(ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public async Task<bool> ValidateTransactionAsync(Transaction transaction)
        {
            var tokenContractAddress =
                _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name);
            if (transaction.To != tokenContractAddress)
            {
                return true;
            }

            TokenContractContainer.TokenContractStub tokenStub; // No need to instantiate.
            var txsGeneratedByPlugins = new List<string>
            {
                nameof(tokenStub.ChargeTransactionFees),
                nameof(tokenStub.ChargeResourceToken),
                nameof(tokenStub.CheckThreshold),
                nameof(tokenStub.CheckResourceToken)
            };
            return !txsGeneratedByPlugins.Contains(transaction.MethodName);
        }
    }
}