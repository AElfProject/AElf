using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.Extensions;
using Google.Protobuf;

namespace AElf.Kernel.Concurrency.Execution
{
	/// <summary>
	/// Used locally to send request to chain executor of the repective chain.
	/// </summary>
	public class ChainRequestor : UntypedActor
	{
		private readonly ActorSystem _system;
		private readonly Hash _chainId;
		private ActorSelection _chainExecutorActorSelection;
		private long _nextRequestId = 0;
		private Dictionary<long, TaskCompletionSource<List<TransactionResult>>> _requestIdToTaskCompleteSource = new Dictionary<long, TaskCompletionSource<List<TransactionResult>>>();

		public ChainRequestor(ActorSystem system, Hash chainId)
		{
			// TODO: Check Chain Executor valid
			_system = system;
			_chainId = chainId;
			_chainExecutorActorSelection = system.ActorSelection("/user/exec/0x" + chainId.ToByteArray().ToHex());
		}

		protected override void OnReceive(object message)
		{
			switch (message)
			{
				case LocalExecuteTransactionsMessage req:
					var reqId = GetNextRequestId();
					_requestIdToTaskCompleteSource.Add(reqId, req.TaskCompletionSource);
					_chainExecutorActorSelection.Tell(new RequestExecuteTransactions(reqId, req.Transactions));
					break;
				case RespondExecuteTransactions res:
					if (_requestIdToTaskCompleteSource.TryGetValue(res.RequestId, out var taskCompletionSource))
					{
						taskCompletionSource.TrySetResult(res.TransactionResults);
					}
					break;
			}
		}

		private long GetNextRequestId()
		{
			return Interlocked.Increment(ref _nextRequestId);
		}

		public static Props Props(ActorSystem system, Hash chainId)
		{
			return Akka.Actor.Props.Create(() => new ChainRequestor(system, chainId));
		}

	}
}
