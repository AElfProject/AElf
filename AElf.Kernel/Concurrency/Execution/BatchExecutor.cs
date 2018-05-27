using System;
using System.Linq;
using System.Collections.Generic;
using Akka.Actor;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.Concurrency.Scheduling;

namespace AElf.Kernel.Concurrency.Execution
{
	/// <summary>
	/// Batch executor groups a list of transactions into groups/jobs and run them in parallel.
	/// </summary>
	public class BatchExecutor : UntypedActor
	{
		public enum ChildType
		{
			Group,
			Job
		}
		enum State
		{
			PendingGrouping,
			ReadyToRun,
			Running
		}
		private State _state = State.PendingGrouping;
		private bool _startExecutionMessageReceived = false;
		private Grouper _grouper = new Grouper();
		private IChainContext _chainContext;
		private List<ITransaction> _transactions;
		private List<List<ITransaction>> _grouped;
		private IActorRef _resultCollector;
		private ChildType _childType;
		private Dictionary<IActorRef, List<ITransaction>> _actorToTransactions = new Dictionary<IActorRef, List<ITransaction>>();
		private Dictionary<Hash, TransactionResult> _transactionResults = new Dictionary<Hash, TransactionResult>();

		public BatchExecutor(IChainContext chainContext, List<ITransaction> transactions, IActorRef resultCollector, ChildType childType)
		{
			_chainContext = chainContext;
			_transactions = transactions;
			_resultCollector = resultCollector;
			_childType = childType;
		}

		protected override void PreStart()
		{
			Context.System.Scheduler.ScheduleTellOnce(new TimeSpan(0, 0, 0), Self, StartGroupingMessage.Instance, Self);
		}

		protected override void OnReceive(object message)
		{
			switch (message)
			{
				case StartGroupingMessage startGrouping:
					if (_state == State.PendingGrouping)
					{
						_grouped = _grouper.Process(_transactions);
						// TODO: Report and/or log grouping outcomes
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
					ForwardResult(res);
					StopIfAllFinished();
					break;
				case Terminated t:
					Context.Unwatch(Sender);
					// For now, just ignore
					// TODO: Handle failure
					break;
			}
		}

		private void CreateChildren()
		{
			foreach (var txs in _grouped)
			{
				IActorRef actor = null;
				if (_childType == ChildType.Group)
				{
					actor = Context.ActorOf(GroupExecutor.Props(_chainContext, txs, Self));
				}
				else
				{
					actor = Context.ActorOf(JobExecutor.Props(_chainContext, txs, Self));
				}

				_actorToTransactions.Add(actor, txs);
				Context.Watch(actor);
			}
		}

		private void MaybeStartChildren()
		{
			if (_state == State.ReadyToRun && _startExecutionMessageReceived)
			{
				foreach (var a in _actorToTransactions.Keys)
				{
					a.Tell(StartExecutionMessage.Instance);
				}
				_state = State.Running;
			}
		}

		private void ForwardResult(TransactionResultMessage resultMessage)
		{
			if (_resultCollector != null)
			{
				_resultCollector.Forward(resultMessage);
			}
		}

		private void StopIfAllFinished()
		{
			if (_transactionResults.Count == _transactions.Count)
			{
				Context.Stop(Self);
			}
		}

		public static Props Props(IChainContext chainContext, List<ITransaction> transactions, IActorRef resultCollector, ChildType childType)
		{
			return Akka.Actor.Props.Create(() => new BatchExecutor(chainContext, transactions, resultCollector, childType));
		}
	}
}
