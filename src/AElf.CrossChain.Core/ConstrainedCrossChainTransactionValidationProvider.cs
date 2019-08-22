using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.CrossChain;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Types;

namespace AElf.CrossChain
{
    public class ConstrainedCrossChainTransactionValidationProvider : ITransactionValidationProvider
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        public ConstrainedCrossChainTransactionValidationProvider(
            ISmartContractAddressService smartContractAddressService)
        {
            _smartContractAddressService = smartContractAddressService;
        }

        public async Task<bool> ValidateTransactionAsync(Transaction transaction)
        {
            var crossChainContractAddress =
                _smartContractAddressService.GetAddressByContractName(CrossChainSmartContractAddressNameProvider.Name);
            var constrainedTransaction = new Lazy<List<string>>(() =>
                new List<string>
                {
                    nameof(CrossChainContractContainer.CrossChainContractStub.RecordCrossChainData),
                });
            if (transaction.To == crossChainContractAddress &&
                constrainedTransaction.Value.Contains(transaction.MethodName))
            {
                return false;
            }

            return true;
        }
    }
}