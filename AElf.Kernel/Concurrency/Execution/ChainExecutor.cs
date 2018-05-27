using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using AElf.Kernel.Services;
using AElf.Kernel.Concurrency.Execution.Messages;

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
        private IChainContext _chainContext;
        private IAccountContextService _accountContextService;
        private IActorRef _currentRequestor;
        private RequestExecuteTransactions _currentRequest;
        private IActorRef _currentExecutor;
        private Dictionary<Hash, TransactionResult> _currentTransactionResults;

        public ChainExecutor(IChainContext chainContext, IAccountContextService accountContextService)
        {
            _chainContext = chainContext;
            _accountContextService = accountContextService;
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case RequestAccountDataContext req:
                    var origSender = Sender;
                    _accountContextService.GetAccountDataContext(req.AccountHash, _chainContext.ChainId).ContinueWith(
                        task => new RespondAccountDataContext(req.RequestId, task.Result),
                        TaskContinuationOptions.AttachedToParent & TaskContinuationOptions.ExecuteSynchronously
                    ).PipeTo(origSender);
                    break;
                case RequestExecuteTransactions req:
                    if (_state == State.Running)
                    {
                        Sender.Tell(new RespondExecuteTransactions(req.RequestId, RespondExecuteTransactions.RequestStatus.Rejected, new List<TransactionResult>()));
                    }
                    else
                    {
                        // Currently only supports one request at a time
                        _currentRequestor = Sender;
                        _currentRequest = req;
                        _currentExecutor = Context.ActorOf(BatchExecutor.Props(_chainContext, req.Transactions, Self, BatchExecutor.ChildType.Group));
                        _currentTransactionResults = new Dictionary<Hash, TransactionResult>();
                        Context.Watch(_currentExecutor);
                        _currentExecutor.Tell(StartExecutionMessage.Instance);
                    }
                    break;
                case TransactionResultMessage res:
                    _currentTransactionResults[res.TransactionResult.TransactionId] = res.TransactionResult;
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
            var txRes = new List<TransactionResult>();
            foreach (var tx in _currentRequest.Transactions)
            {
                var txId = tx.GetHash();
                if (!_currentTransactionResults.TryGetValue(txId, out var r))
                {
                    // TODO: Assuming Status.ExecutedFailed may not be correct
                    r = new TransactionResult()
                    {
                        TransactionId = txId,
                        Status = Status.ExecutedFailed
                    };
                }
                txRes.Add(r);
            }
            var response = new RespondExecuteTransactions(_currentRequest.RequestId, RespondExecuteTransactions.RequestStatus.Executed, txRes);
            _currentRequestor.Tell(response);
            _state = State.Idle;
        }

        public static Props Props(IChainContext chainContext, IAccountContextService accountContextService)
        {
            return Akka.Actor.Props.Create(() => new ChainExecutor(chainContext, accountContextService));
        }

    }
}
