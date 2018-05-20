using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
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
			_requestor = system.ActorOf(ParallelExecutionGeneralRequestor.Props(system));
		}

		public async Task ExecuteAsync(List<Transaction> transactions, Hash chainId)
		{
			TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
			_requestor.Tell(new LocalExecuteTransactionsMessage(chainId, transactions, taskCompletionSource));
			await taskCompletionSource.Task;
		}
        
		// TODO: Maybe we need a finalizer
	}
}
