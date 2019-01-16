using System;
using AElf.ChainController;
using AElf.Kernel.Managers;
using AElf.SmartContract;

namespace AElf.Execution.Execution
{
    public class NoFeeSimpleExecutingService : SimpleExecutingService
    {
        public NoFeeSimpleExecutingService(ISmartContractService smartContractService,
            ITransactionTraceManager transactionTraceManager, IStateManager stateManager,
            IChainContextService chainContextService) : base(smartContractService, transactionTraceManager,
            stateManager, chainContextService)
        {
            TransactionFeeDisabled = true;
        }
    }
}