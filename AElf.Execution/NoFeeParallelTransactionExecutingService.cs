using System;
using AElf.Execution.Execution;
using AElf.Execution.Scheduling;

namespace AElf.Execution
{
    public class NoFeeParallelTransactionExecutingService : ParallelTransactionExecutingService
    {
        public NoFeeParallelTransactionExecutingService(IActorEnvironment actorEnvironment, IGrouper grouper,
            ServicePack servicePack) : base(actorEnvironment, grouper, servicePack)
        {
            TransactionFeeDisabled = true;
        }
    }
}