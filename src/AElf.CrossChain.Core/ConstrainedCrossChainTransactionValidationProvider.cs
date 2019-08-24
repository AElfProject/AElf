using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.CrossChain;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;

namespace AElf.CrossChain
{
    public class ConstrainedCrossChainTransactionValidationProvider : ITransactionValidationProvider
    {
        private readonly ISmartContractAddressService _smartContractAddressService;

        public ILogger<ConstrainedCrossChainTransactionValidationProvider> Logger { get; set; }

        private bool _alreadyHas;

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
                if (!_alreadyHas)
                {
                    _alreadyHas = true;
                    return true;
                }
                Logger.LogError($"Not allowed to call cross chain contract method '{transaction.MethodName}'");
                _alreadyHas = false;
                return false;
            }

            return true;
        }
    }
}