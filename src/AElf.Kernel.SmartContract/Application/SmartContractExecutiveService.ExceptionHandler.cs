using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;

namespace AElf.Kernel.SmartContract.Application;

public partial class SmartContractExecutiveService
{
    protected virtual async Task HandleExceptionWhileGettingSmartContractRegistration(
        IChainContext chainContext, Address address, IExecutive executiveZero = null)
    {
        if (executiveZero != null)
            await PutExecutiveAsync(chainContext, _defaultContractZeroCodeProvider.ContractZeroAddress,
                executiveZero);
    }
}