using System;
using AElf.Kernel.SmartContractExecution.Execution;
using AElf.Kernel.SmartContractExecution.Scheduling;
using Microsoft.Extensions.Options;

namespace AElf.Kernel.SmartContractExecution
{
    public class NoFeeParallelTransactionExecutingService : ParallelTransactionExecutingService
    {
        public NoFeeParallelTransactionExecutingService(IActorEnvironment actorEnvironment, IGrouper grouper,
            ServicePack servicePack, IOptionsSnapshot<ExecutionOptions> options) : base(actorEnvironment, grouper, servicePack, options)
        {
            TransactionFeeDisabled = true;
        }
    }
}