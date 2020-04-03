using AElf.CSharp.Core;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application
{
    public class ContractReaderFactory : IContractReaderFactory, ITransientDependency
    {
        private readonly ITransactionReadOnlyExecutionService _transactionReadOnlyExecutionService;

        public ContractReaderFactory(ITransactionReadOnlyExecutionService transactionReadOnlyExecutionService)
        {
            _transactionReadOnlyExecutionService = transactionReadOnlyExecutionService;
        }

        public T Create<T>(ContractReaderContext contractReaderContext)
            where T : ContractStubBase, new()
        {
            var methodStubFactory = new ReadOnlyMethodStubFactory(_transactionReadOnlyExecutionService);
            methodStubFactory.SetContractReadContext(contractReaderContext);

            return new T()
            {
                __factory = methodStubFactory
            };
        }
    }
}