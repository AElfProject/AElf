using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using AElf.Kernel;
using AElf.Kernel.Concurrency.Execution;
using AElf.Kernel.Concurrency.Execution.Messages;

namespace AElf.Kernel.Concurrency
{
	public class ParallelTransactionExecutingService : IParallelTransactionExecutingService
	{
		private readonly ActorSystem _system;
		private IActorRef _requestor;

		public ParallelTransactionExecutingService(ActorSystem system)
		{
			_system = system;
			_requestor = system.ActorOf(GeneralRequestor.Props(system));
		}

		public async Task<List<TransactionResult>> ExecuteAsync(List<ITransaction> transactions, Hash chainId)
		{
			var taskCompletionSource = new TaskCompletionSource<List<TransactionResult>>();
			_requestor.Tell(new LocalExecuteTransactionsMessage(chainId, transactions, taskCompletionSource));
			return await taskCompletionSource.Task;
		}
        
		// TODO: Maybe we need a finalizer
	}
}
