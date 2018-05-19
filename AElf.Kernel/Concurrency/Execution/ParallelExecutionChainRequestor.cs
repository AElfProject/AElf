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
	public class ParallelExecutionChainRequestor : UntypedActor
	{
		private readonly ActorSystem _system;
		private readonly Hash _chainId;
		private ActorSelection _chainExecutor;
		private long _nextRequestId = 0;
		private Dictionary<long, TaskCompletionSource<bool>> _requestIdToTaskCompleteSource = new Dictionary<long, TaskCompletionSource<bool>>();

		public ParallelExecutionChainRequestor(ActorSystem system, Hash chainId)
		{
			// TODO: Check Chain Executor valid
			_system = system;
			_chainId = chainId;
			_chainExecutor = system.ActorSelection("/user/chainexecutor-" + chainId.ToByteArray().ToHex());
		}

		protected override void OnReceive(object message)
		{
			switch (message)
			{
				case ExecuteTransactionsMessageToLocalChainRequestor req:
					var reqId = GetNextRequestId();
					_requestIdToTaskCompleteSource.Add(reqId, req.TaskCompletionSource);
					_chainExecutor.Tell(new RequestExecuteTransactions(reqId, req.Transactions));
					break;
				case RespondExecuteTransactions res when Sender.Equals(_chainExecutor):
					if (_requestIdToTaskCompleteSource.TryGetValue(res.RequestId, out var taskCompletionSource))
					{
						taskCompletionSource.TrySetResult(true);
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
			return Akka.Actor.Props.Create(() => new ParallelExecutionChainRequestor(system, chainId));
		}

	}
}
