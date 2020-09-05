using System.ComponentModel;
using System.Threading.Tasks;
using AElf.Standards.ACS0;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel.SmartContractExecution.Application
{
    public interface ISmartContractRegistrationInStateProvider
    {
        Task<SmartContractRegistration> GetSmartContractRegistrationAsync(IChainContext chainContext, Address address);
    }
    
    public class SmartContractRegistrationInStateProvider : ISmartContractRegistrationInStateProvider
    {
        private readonly IDefaultContractZeroCodeProvider _defaultContractZeroCodeProvider;
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;

        public SmartContractRegistrationInStateProvider(IDefaultContractZeroCodeProvider defaultContractZeroCodeProvider,
            ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService)
        {
            _defaultContractZeroCodeProvider = defaultContractZeroCodeProvider;
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
        }

        public async Task<SmartContractRegistration> GetSmartContractRegistrationAsync(IChainContext chainContext,Address address)
        {
            var zeroAddress = _defaultContractZeroCodeProvider.ContractZeroAddress;
            var tx = new Transaction
            {
                From = zeroAddress,
                To = zeroAddress,
                MethodName = nameof(ACS0Container.ACS0Stub.GetSmartContractRegistrationByAddress),
                Params = address.ToByteString()
            };

            return await _transactionReadOnlyExecutionService.ExecuteAsync<SmartContractRegistration>(
                chainContext, tx, TimestampHelper.GetUtcNow(), false);
        }
    }
}