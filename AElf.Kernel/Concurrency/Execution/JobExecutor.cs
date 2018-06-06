using System;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.KernelAccount;
using Google.Protobuf;

namespace AElf.Kernel.Concurrency.Execution
{
    /// <summary>
    /// Job executor runs a list of transactions sequentially.
    /// </summary>
    public class JobExecutor : UntypedActor
    {
        enum State
        {
            Initializing,
            NotStarted,
            Running
        }
        private State _state = State.Initializing;
        private bool _startExecutionMessageReceived = false;
        private Hash _chainId;
        private IActorRef _serviceRouter;
        private ServicePack _servicePack;
        private IActorRef _resultCollector;
        private List<ITransaction> _transactions;
        private int _currentRunningIndex = -1;
        private Hash _currentTransactionHash;
        private IChainContext _chainContext;
        private Dictionary<Hash, TransactionResult> _transactionResults = new Dictionary<Hash, TransactionResult>();

        public JobExecutor(Hash chainId, IActorRef serviceRouter, List<ITransaction> transactions, IActorRef resultCollector)
        {
            _chainId = chainId;
            _serviceRouter = serviceRouter;
            _transactions = transactions;
            _resultCollector = resultCollector;
        }

        protected override void PreStart()
        {
            if (_transactions.Count == 0)
            {
                Context.System.Scheduler.ScheduleTellOnce(new TimeSpan(0, 0, 0), Self, PoisonPill.Instance, Self);
            }
            else
            {
                Context.System.Scheduler.ScheduleTellOnce(new TimeSpan(0, 0, 0), _serviceRouter, new RequestLocalSerivcePack(0), Self);
            }
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case RespondLocalSerivcePack res:
                    if (_state == State.Initializing)
                    {
                        _servicePack = res.ServicePack;
                        _state = State.NotStarted;
                        if (_startExecutionMessageReceived)
                        {
                            RunNextOrStop();
                        }
                    }
                    break;
                case StartExecutionMessage start:
                    _startExecutionMessageReceived = true;
                    if (_state == State.NotStarted)
                    {
                        RunNextOrStop();
                    }
                    break;
                case TransactionResultMessage res when res.TransactionResult.TransactionId == _currentTransactionHash:
                    if (_state == State.Running)
                    {
                        ForwardResult(res);
                        _transactionResults.Add(res.TransactionResult.TransactionId, res.TransactionResult);
                        RunNextOrStop();
                    }
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
            if (_chainContext == null)
            {
                _chainContext = await _servicePack.ChainContextService.GetChainContextAsync(_chainId);
            }

            var executive = await _servicePack.SmartContractService.GetExecutiveAsync(transaction.To, _chainId);
            // TODO: Handle timeout
            TransactionResult result = new TransactionResult()
            {
                TransactionId = transaction.GetHash(),
                Status = Status.Pending
            };
            // TODO: Reject tx if IncrementId != Nonce

            var txCtxt = new TransactionContext()
            {
                PreviousBlockHash = _chainContext.BlockHash,
                Transaction = transaction
            };

            try
            {

                await executive.SetTransactionContext(txCtxt).Apply();
                result.Logs.AddRange(txCtxt.Trace.FlattenedLogs);
                // TODO: Check run results / logs etc.
                result.Status = Status.Mined;
            }
            catch (Exception ex)
            {
                // TODO: Improve log
                txCtxt.Trace.StdErr += ex.ToString() + "\n";
                //result.Logs = ByteString.CopyFrom(Encoding.ASCII.GetBytes(ex.ToString()));
                result.Status = Status.ExecutedFailed;
            }
            finally
            {
                await _servicePack.SmartContractService.PutExecutiveAsync(transaction.To, executive);
            }

            return result;
        }

        public static Props Props(Hash chainId, IActorRef serviceRouter, List<ITransaction> transactions, IActorRef resultCollector)
        {
            return Akka.Actor.Props.Create(() => new JobExecutor(chainId, serviceRouter, transactions, resultCollector));
        }
    }
}
