using AElf.CSharp.Core;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public class ContractReaderFactory<T> : IContractReaderFactory<T>
        where T : ContractStubBase, new()
    {
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;

        public ContractReaderFactory(ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService)
        {
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
        }

        public T Create(ContractReaderContext contractReaderContext)
        {
            var methodStubFactory = new ReadOnlyMethodStubFactory(_transactionReadOnlyExecutionService);
            methodStubFactory.SetContractReaderContext(contractReaderContext);

            return new T()
            {
                __factory = methodStubFactory
            };
        }
    }
}