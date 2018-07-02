﻿using System;
using System.Collections.Generic;
using AElf.Kernel.Concurrency.Scheduling;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.Types;
using Akka.Actor;
using Akka.Util.Internal;

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
        private Hash _chainId;
        private IActorRef _serviceRouter;
        private IActorRef _resultCollector;
        private List<ITransaction> _transactions;
        private List<List<ITransaction>> _batched;
        private int _currentRunningIndex = -1;
        private List<IActorRef> _actors = new List<IActorRef>();
        private Dictionary<Hash, TransactionTrace> _transactionTraces = new Dictionary<Hash, TransactionTrace>();
        private Exception _batchingException;

        public GroupExecutor(Hash chainId, IActorRef serviceRouter, List<ITransaction> transactions, IActorRef resultCollector)
        {
            _chainId = chainId;
            _serviceRouter = serviceRouter;
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
                        _batchingException = null;
                        try
                        {
                            _batched = _batcher.Process(_transactions);
                            // TODO: Report and/or log batching outcomes
                            CreateChildren();
                            _state = State.ReadyToRun;
                        }
                        catch (Exception e)
                        {
                            _batchingException = e;
                        }
                        
                        RunNextOrStop();
                    }
                    break;
                case StartExecutionMessage start:
                    _startExecutionMessageReceived = true;
                    RunNextOrStop();
                    break;
                case TransactionTraceMessage res:
                    var txnId = res.TransactionTrace.TransactionId;
                    if (!_transactionTraces.ContainsKey(txnId))
                    {
                        _transactionTraces[txnId] = res.TransactionTrace;
                        ForwardResult(res);                        
                    }
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
                var actor = Context.ActorOf(JobExecutor.Props(_chainId, _serviceRouter, txs, _resultCollector));
                _actors.Add(actor);
                Context.Watch(actor);
            }
        }

        private void RunNextOrStop()
        {
            if (_batchingException != null && _startExecutionMessageReceived)
            {
                foreach (var txn in _transactions)
                {
                    var traceMsg = new TransactionTraceMessage(
                        new TransactionTrace()
                        {
                            TransactionId = txn.GetHash() 
                        }
                    );
                    traceMsg.TransactionTrace.StdErr += _batchingException + "\n";
                    ForwardResult(traceMsg);
                }
                Context.Stop(Self);
            }
            
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

        private void ForwardResult(TransactionTraceMessage traceMessage)
        {
            if (_resultCollector != null)
            {
                _resultCollector.Forward(traceMessage);
            }
        }

        public static Props Props(Hash chainId, IActorRef serviceRouter, List<ITransaction> transactions, IActorRef resultCollector)
        {
            return Akka.Actor.Props.Create(() => new GroupExecutor(chainId, serviceRouter, transactions, resultCollector));
        }

    }
}
