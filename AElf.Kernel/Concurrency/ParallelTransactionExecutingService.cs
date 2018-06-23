using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using System.Threading;
using Akka.Actor;
using AElf.Kernel;
using AElf.Kernel.Concurrency.Execution;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.Concurrency.Scheduling;

namespace AElf.Kernel.Concurrency
{
	public class ParallelTransactionExecutingService : IParallelTransactionExecutingService
	{
		private readonly IGrouper _grouper;
		private readonly IActorRef _requestor;

		public ParallelTransactionExecutingService(IActorRef requestor, IGrouper grouper)
		{
			_requestor = requestor;
			_grouper = grouper;
		}

		public async Task<List<TransactionTrace>> ExecuteAsync(List<ITransaction> transactions, Hash chainId)
		{
			List<TaskCompletionSource<List<TransactionTrace>>> taskCompletionSources= new List<TaskCompletionSource<List<TransactionTrace>>>();
				
			// TODO: Flexible timeout setting
			using (new Timer(
				CancelExecutions,new object(), TimeSpan.FromMilliseconds(3500),
				TimeSpan.FromMilliseconds(-1)
			))
			{
				foreach (var group in _grouper.Process(transactions))
				{
					var tcs = new TaskCompletionSource<List<TransactionTrace>>();
					_requestor.Tell(new LocalExecuteTransactionsMessage(chainId, group, tcs));
					taskCompletionSources.Add(tcs);
				}
			}
			
//			foreach (var tcs in taskCompletionSources)
//			{
//				tcs.Task.Wait();
//			}
//			
			var results = await Task.WhenAll(taskCompletionSources.Select(x=>x.Task).ToArray());

			return results.SelectMany(x => x).ToList();
//			return await Task.FromResult( taskCompletionSources.Select(x=>x.Task.Result).SelectMany(x=>x).ToList());
		}


		private void CancelExecutions(object stateInfo)
		{
			_requestor.Tell(JobExecutionCancelMessage.Instance);
		}
		// TODO: Maybe we need a finalizer
	}
}
