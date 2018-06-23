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
			Initializing,
			ReadyToRun,
			Running
		}
        private State _state = State.Initializing;
		private bool _startExecutionMessageReceived = false;
		private Grouper _grouper;
        private Hash _chainId;
        private IActorRef _serviceRouter;
        private ServicePack _servicePack;
		private List<ITransaction> _transactions;
		private List<List<ITransaction>> _grouped;
        private IActorRef _resultCollector;
		private ChildType _childType;
		private Dictionary<IActorRef, List<ITransaction>> _actorToTransactions = new Dictionary<IActorRef, List<ITransaction>>();
		private Dictionary<Hash, TransactionTrace> _transactionTraces = new Dictionary<Hash, TransactionTrace>();
		private Exception _groupingException;

        public BatchExecutor(Hash chainId, IActorRef serviceRouter, List<ITransaction> transactions, IActorRef resultCollector, ChildType childType)
		{
            _chainId = chainId;
            _serviceRouter = serviceRouter;
			_transactions = transactions;
            _resultCollector = resultCollector;
			_childType = childType;
		}

		protected override void PreStart()
		{
            Context.System.Scheduler.ScheduleTellOnce(new TimeSpan(0, 0, 0), _serviceRouter, new RequestLocalSerivcePack(0), Self);
		}

		protected override void OnReceive(object message)
		{
			switch (message)
			{
                case RespondLocalSerivcePack res:
                    if (_state == State.Initializing)
                    {
                        _servicePack = res.ServicePack;
	                    try
	                    {
		                    _groupingException = null;
		                    _grouper = new Grouper(_servicePack.ResourceDetectionService);
		                    _grouped = _grouper.SimpleProcessWithCoreCount(4, _chainId, _transactions); //4 is core count, for test it's constant, neet to somehow accquire this core count of the BP willing to give
		                    // TODO: Report and/or log grouping outcomes
		                    CreateChildren();
		                    _state = State.ReadyToRun;
	                    }
	                    catch (Exception e)
	                    {
		                    _groupingException = e;
	                    }
                        
                        MaybeStartChildren();
                    }
                    break;
				case StartExecutionMessage start:
					_startExecutionMessageReceived = true;
					MaybeStartChildren();
					break;
				case TransactionTraceMessage res:
					_transactionTraces[res.TransactionTrace.TransactionId] = res.TransactionTrace;
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
                    actor = Context.ActorOf(GroupExecutor.Props(_chainId, _serviceRouter, txs, Self));
				}
				else
				{
                    actor = Context.ActorOf(JobExecutor.Props(_chainId, _serviceRouter, txs, Self));
				}

				_actorToTransactions.Add(actor, txs);
				Context.Watch(actor);
			}
		}

		private void MaybeStartChildren()
		{
			if (_groupingException != null && _startExecutionMessageReceived)
			{
				foreach (var txn in _transactions)
				{
					var traceMsg = new TransactionTraceMessage(
						new TransactionTrace()
						{
							TransactionId = txn.GetHash() 
						}
					);
					traceMsg.TransactionTrace.StdErr += _groupingException + "\n";
					ForwardResult(traceMsg);
				}
				Context.Stop(Self);
			}
			
			if (_state == State.ReadyToRun && _startExecutionMessageReceived)
			{
				foreach (var a in _actorToTransactions.Keys)
				{
					a.Tell(StartExecutionMessage.Instance);
				}
				_state = State.Running;
			}
		}

		private void ForwardResult(TransactionTraceMessage traceMessage)
		{
            if (_resultCollector != null)
			{
                _resultCollector.Forward(traceMessage);
			}
		}

		private void StopIfAllFinished()
		{
			if (_transactionTraces.Count == _transactions.Count)
			{
				Context.Stop(Self);
			}
		}

        public static Props Props(Hash chainId, IActorRef serviceRouter, List<ITransaction> transactions, IActorRef resultCollector, ChildType childType)
		{
            return Akka.Actor.Props.Create(() => new BatchExecutor(chainId, serviceRouter, transactions, resultCollector, childType));
		}
	}
}
