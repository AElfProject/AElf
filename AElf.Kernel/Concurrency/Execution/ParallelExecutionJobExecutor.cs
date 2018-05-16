using System;
using System.Linq;
using System.Collections.Generic;
using Akka.Actor;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.Concurrency;

namespace AElf.Kernel.Concurrency.Execution
{
	// TODO: Use real job class
	using Job = List<Transaction>;

	public class ParallelExecutionJobExecutor : UntypedActor
	{
		private IChainContext _chainContext;
		private Job _job;
		private IActorRef _resultCollector;
		private Dictionary<IActorRef, Transaction> _actorToTransaction = new Dictionary<IActorRef, Transaction>();
		private Dictionary<Hash, TransactionResult> _transactionResults = new Dictionary<Hash, TransactionResult>();

		public ParallelExecutionJobExecutor(IChainContext chainContext, Job job, IActorRef resultCollector)
		{
			_chainContext = chainContext;
			_job = job;
			_resultCollector = resultCollector;
		}

		protected override void PreStart()
		{
			foreach (var tx in _job)
			{
				var actor = Context.ActorOf(ParallelExecutionTransactionExecutor.Props(_chainContext, tx, Self));
				_actorToTransaction.Add(actor, tx);
			}

		}

		protected override void OnReceive(object message)
		{
			switch (message)
			{
				case StartExecutionMessage start:
					foreach (var a in _actorToTransaction.Keys)
					{
						a.Tell(start);
					}
					break;
				case TransactionResultMessage res:
					_transactionResults[res.TransactionResult.TransactionId] = res.TransactionResult;
					CheckReceived();
					break;
				case Terminated t:
					var txId = _actorToTransaction[Sender].GetHash();
					if (!_transactionResults.ContainsKey(txId))
					{
						_transactionResults.Add(txId, new TransactionResult { TransactionId = txId, Status = Status.ExecutedFailed });
					}
					CheckReceived();
					break;
			}
		}

		private void CheckReceived()
		{
			if (_transactionResults.Count == _actorToTransaction.Count)
			{
				if (_resultCollector != null)
				{
					_resultCollector.Tell(new JobResultMessage(_transactionResults.Values.ToList()));
				}
				Context.Stop(Self);
			}
		}

		public static Props Props(IChainContext chainContext, Job job, IActorRef resultCollector)
		{
			return Akka.Actor.Props.Create(() => new ParallelExecutionJobExecutor(chainContext, job, resultCollector));
		}
	}
}
