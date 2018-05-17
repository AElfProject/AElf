using System;
using System.Linq;
using System.Collections.Generic;
using Akka.Actor;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.Concurrency;

namespace AElf.Kernel.Concurrency.Execution
{
	// Inside Batch, there are Jobs that will be run in parallel
	using Transactions = List<Transaction>;

	interface IGrouper
	{
		List<List<Transaction>> Process(List<Transaction> transactions);
	}

	public class ParallelExecutionBatchExecutor : UntypedActor
	{
		enum State
		{
			PendingGrouping,
			GroupingDone,
			CreatingChildren,
			ReadyToRun,
			Running
		}
		private State _state = State.PendingGrouping;
		private bool _startExecutionMessageReceived = false;
		private IGrouper _grouper;
		private IChainContext _chainContext;
		private Transactions _transactions;
		private List<Transactions> _grouped;
		private IActorRef _resultCollector;
		private Dictionary<IActorRef, Transaction> _actorToTransaction = new Dictionary<IActorRef, Transaction>();
		private Dictionary<Hash, TransactionResult> _transactionResults = new Dictionary<Hash, TransactionResult>();

		public ParallelExecutionBatchExecutor(IChainContext chainContext, Transactions transactions, IActorRef resultCollector)
		{
			_chainContext = chainContext;
			_transactions = transactions;
			_resultCollector = resultCollector;
		}

		protected override void PreStart()
		{
			Context.System.Scheduler.ScheduleTellOnce(new TimeSpan(0, 0, 0), Self, new StartGroupingMessage(), Self);
		}

		protected override void OnReceive(object message)
		{
			switch (message)
			{
				case StartGroupingMessage startGrouping:
					if (_state == State.PendingGrouping)
					{
						_grouped = _grouper.Process(_transactions);
						_state = State.CreatingChildren;
						CreateChildren();
						_state = State.ReadyToRun;
						MaybeStartChildren();
					}
					break;
				case StartExecutionMessage start:
					_startExecutionMessageReceived = true;
					MaybeStartChildren();
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

		private void CreateChildren()
		{
			foreach (var tx in _transactions)
			{
				var actor = Context.ActorOf(ParallelExecutionTransactionExecutor.Props(_chainContext, tx, Self));
				_actorToTransaction.Add(actor, tx);
			}
		}

		private void MaybeStartChildren()
		{
			if (_state == State.Running)
				return;
			if (_state == State.ReadyToRun && _startExecutionMessageReceived)
			{
				foreach (var a in _actorToTransaction.Keys)
				{
					a.Tell(new StartExecutionMessage());
				}
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

		public static Props Props(IChainContext chainContext, Transactions job, IActorRef resultCollector)
		{
			return Akka.Actor.Props.Create(() => new ParallelExecutionBatchExecutor(chainContext, job, resultCollector));
		}
	}
}
