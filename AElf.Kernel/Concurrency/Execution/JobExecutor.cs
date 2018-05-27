using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.KernelAccount;

namespace AElf.Kernel.Concurrency.Execution
{
    /// <summary>
    /// Job executor runs a list of transactions sequentially.
    /// </summary>
    public class JobExecutor : UntypedActor
    {
        enum State
        {
            NotStarted,
            Running
        }
        private State _state = State.NotStarted;
        private IChainContext _chainContext;
        private IActorRef _resultCollector;
        private List<ITransaction> _transactions;
        private int _currentRunningIndex = -1;
        private Hash _currentTransactionHash;
        private Dictionary<Hash, TransactionResult> _transactionResults = new Dictionary<Hash, TransactionResult>();

        public JobExecutor(IChainContext chainContext, List<ITransaction> transactions, IActorRef resultCollector)
        {
            _chainContext = chainContext;
            _transactions = transactions;
            _resultCollector = resultCollector;
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case StartExecutionMessage start:
                    if (_state == State.NotStarted)
                    {
                        RunNextOrStop();
                    }
                    break;
                case TransactionResultMessage res when res.TransactionResult.TransactionId == _currentTransactionHash:
                    ForwardResult(res);
                    _transactionResults.Add(res.TransactionResult.TransactionId, res.TransactionResult);
                    RunNextOrStop();
                    break;
            }
        }

        private void ForwardResult(TransactionResultMessage resultMessage)
        {
            if (_resultCollector != null)
            {
                _resultCollector.Forward(resultMessage);
            }
        }

        private void RunNextOrStop()
        {
            _state = State.Running;
            if (_currentRunningIndex == _transactions.Count - 1)
            {
                Context.Stop(Self);
            }
            else
            {
                _currentRunningIndex++;
                var tx = _transactions[_currentRunningIndex];
                _currentTransactionHash = tx.GetHash();
                ExecuteTransaction(tx).ContinueWith(
                    task => new TransactionResultMessage(task.Result),
                    TaskContinuationOptions.AttachedToParent & TaskContinuationOptions.ExecuteSynchronously
                ).PipeTo(Self);
            }
        }

        private async Task<TransactionResult> ExecuteTransaction(ITransaction transaction)
        {
            // TODO: Handle timeout
            ISmartContractZero smartContractZero = _chainContext.SmartContractZero;
            TransactionResult result = new TransactionResult();
            result.TransactionId = transaction.GetHash();
            // TODO: Reject tx if IncrementId != Nonce

            try
            {
                await smartContractZero.InvokeAsync(new SmartContractInvokeContext()
                {
                    Caller = transaction.From,
                    MethodName = transaction.MethodName,
                    Params = transaction.Params
                });
                result.Status = Status.Mined;
            }
            catch
            {
                result.Status = Status.ExecutedFailed;
            }

            return result;
        }

        public static Props Props(IChainContext chainContext, List<ITransaction> transactions, IActorRef resultCollector)
        {
            return Akka.Actor.Props.Create(() => new JobExecutor(chainContext, transactions, resultCollector));
        }
    }
}
