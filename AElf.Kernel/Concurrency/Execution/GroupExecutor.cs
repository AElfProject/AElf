using System;
using System.Collections.Generic;
using AElf.Kernel.Concurrency.Scheduling;
using AElf.Kernel.Concurrency.Execution.Messages;
using Akka.Actor;

namespace AElf.Kernel.Concurrency.Execution
{
	/// <summary>
	/// Group executor puts a list of transactions into batchs and run them in sequence.
	/// </summary>
	public class GroupExecutor : UntypedActor
	{
		enum State
		{
			PendingBatching,
			ReadyToRun,
			Running
		}
		private State _state = State.PendingBatching;
		private bool _startExecutionMessageReceived = false;
		private Batcher _batcher = new Batcher();
		private IChainContext _chainContext;
		private IActorRef _resultCollector;
		private List<ITransaction> _transactions;
		private List<List<ITransaction>> _batched;
		private int _currentRunningIndex = -1;
		private List<IActorRef> _actors = new List<IActorRef>();
		private Dictionary<Hash, TransactionResult> _transactionResults = new Dictionary<Hash, TransactionResult>();

		public GroupExecutor(IChainContext chainContext, List<ITransaction> transactions, IActorRef resultCollector)
		{
			_chainContext = chainContext;
			_transactions = transactions;
			_resultCollector = resultCollector;
		}

		protected override void PreStart()
		{
			Context.System.Scheduler.ScheduleTellOnce(new TimeSpan(0, 0, 0), Self, StartBatchingMessage.Instance, Self);
		}

		protected override void OnReceive(object message)
		{
			switch (message)
			{
				case StartBatchingMessage startBatching:
					if (_state == State.PendingBatching)
					{
						_batched = _batcher.Process(_transactions);
						// TODO: Report and/or log batching outcomes
						CreateChildren();
						_state = State.ReadyToRun;
						RunNextOrStop();
					}
					break;
				case StartExecutionMessage start:
					_startExecutionMessageReceived = true;
					RunNextOrStop();
					break;
				case TransactionResultMessage res:
					_transactionResults[res.TransactionResult.TransactionId] = res.TransactionResult;
					ForwardResult(res);
					break;
				case Terminated t:
					Context.Unwatch(Sender);
					if (Sender.Equals(_actors[_currentRunningIndex]))
					{
						RunNextOrStop();
					}
					// TODO: Handle failure
					break;
			}
		}

		private void CreateChildren()
		{
			foreach (var txs in _batched)
			{
				var actor = Context.ActorOf(JobExecutor.Props(_chainContext, txs, Self));
				_actors.Add(actor);
				Context.Watch(actor);
			}
		}

		private void RunNextOrStop()
		{
			if (_state == State.ReadyToRun && _startExecutionMessageReceived || _state == State.Running)
			{
				_state = State.Running;
				if (_currentRunningIndex == _actors.Count - 1)
				{
					Context.Stop(Self);
				}
				else
				{
					_currentRunningIndex++;
					_actors[_currentRunningIndex].Tell(StartExecutionMessage.Instance);
				}
			}
		}

		private void ForwardResult(TransactionResultMessage resultMessage)
        {
            if (_resultCollector != null)
            {
                _resultCollector.Forward(resultMessage);
            }
        }

		public static Props Props(IChainContext chainContext, List<ITransaction> transactions, IActorRef resultCollector)
		{
			return Akka.Actor.Props.Create(() => new GroupExecutor(chainContext, transactions, resultCollector));
		}

	}
}
