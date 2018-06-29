using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using AElf.Kernel.Services;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.Types;

namespace AElf.Kernel.Concurrency.Execution
{
    public class ChainExecutor : UntypedActor
    {
        enum State
        {
            Idle,
            Running,
        }
        private State _state = State.Idle;
        private Hash _chainId;
        private IChainContextService _chainContextService;
        private IAccountContextService _accountContextService;
        private IActorRef _serviceRouter;
        private IActorRef _currentRequestor;
        private RequestExecuteTransactions _currentRequest;
        private IActorRef _currentExecutor;
        private Dictionary<Hash, TransactionTrace> _currentTransactionTraces;

        public ChainExecutor(Hash chainId, IActorRef serviceRouter)
        {
            _chainId = chainId;
            _serviceRouter = serviceRouter;
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case RequestExecuteTransactions req:
                    if (_state == State.Running)
                    {
                        Sender.Tell(new RespondExecuteTransactions(req.RequestId, RespondExecuteTransactions.RequestStatus.Rejected, new List<TransactionTrace>()));
                    }
                    else
                    {
                        _state = State.Running;
                        // Currently only supports one request at a time
                        _currentRequestor = Sender;
                        _currentRequest = req;
                        _currentExecutor = Context.ActorOf(BatchExecutor.Props(_chainId, _serviceRouter, req.Transactions, Self, BatchExecutor.ChildType.Group));
                        _currentTransactionTraces = new Dictionary<Hash, TransactionTrace>();
                        Context.Watch(_currentExecutor);
                        _currentExecutor.Tell(StartExecutionMessage.Instance);
                    }
                    break;
                case TransactionTraceMessage res:
                    _currentTransactionTraces[res.TransactionTrace.TransactionId] = res.TransactionTrace;
                    break;
                case Terminated t when Sender.Equals(_currentExecutor):
                    Context.Unwatch(_currentExecutor);
                    RespondToCurrentRequestorAndSetIdle();
                    break;
                    // TODO: More messages
            }
        }

        private void RespondToCurrentRequestorAndSetIdle()
        {
            var traces = new List<TransactionTrace>();
            foreach (var tx in _currentRequest.Transactions)
            {
                var txId = tx.GetHash();
                if (!_currentTransactionTraces.TryGetValue(txId, out var trace))
                {
                    // TODO: Assuming Status.ExecutedFailed may not be correct
                    trace = new TransactionTrace()
                    {
                        TransactionId = txId,
                        StdErr = "Failed"
                    };
                }
                traces.Add(trace);
            }
            var response = new RespondExecuteTransactions(_currentRequest.RequestId, RespondExecuteTransactions.RequestStatus.Executed, traces);
            _currentRequestor.Tell(response);
            _state = State.Idle;
        }

        public static Props Props(Hash chainId, IActorRef serviceRouter)
        {
            return Akka.Actor.Props.Create(() => new ChainExecutor(chainId, serviceRouter));
        }

    }
}
