using System;
using AElf.Execution.Execution;
using AElf.Execution.Scheduling;
using Microsoft.Extensions.Options;

namespace AElf.Execution
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