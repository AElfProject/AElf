using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.KernelAccount;
using Akka.IO;
using Akka.Routing;
using Google.Protobuf;

namespace AElf.Kernel.Concurrency.Execution
{
    /// <summary>
    /// A worker that runs a list of transactions sequentially.
    /// </summary>
    public class Worker : UntypedActor
    {
        public enum State
        {
            PendingSetSericePack,
            Idle,
            Running,
            Suspended // TODO: Support suspend
        }

        private State _state = State.PendingSetSericePack;
        private long _servingRequestId = -1;

        private ServicePack _servicePack;

        // TODO: Add cancellation
        private CancellationTokenSource _cancellationTokenSource;

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case LocalSerivcePack res:
                    if (_state == State.PendingSetSericePack)
                    {
                        _servicePack = res.ServicePack;
                        _state = State.Idle;
                    }

                    break;
                case JobExecutionRequest req:
                    if (_state == State.Idle)
                    {
                        Sender.Tell(new JobExecutionStatus(req.RequestId, JobExecutionStatus.RequestStatus.Running));
                        RunJob(req).ContinueWith(
                            task => task.Result
                        ).PipeTo(Self);
                    }
                    else if (_state == State.PendingSetSericePack)
                    {
                        Sender.Tell(new JobExecutionStatus(req.RequestId,
                            JobExecutionStatus.RequestStatus.FailedDueToWorkerNotReady));
                    }
                    else
                    {
                        Sender.Tell(new JobExecutionStatus(req.RequestId, JobExecutionStatus.RequestStatus.Rejected));
                    }

                    break;
                case JobExecutionCancelMessage c:
                    _cancellationTokenSource?.Cancel();
                    break;
                case JobExecutionStatusQuery query:
                    if (query.RequestId != _servingRequestId)
                    {
                        Sender.Tell(new JobExecutionStatus(query.RequestId,
                            JobExecutionStatus.RequestStatus.InvalidRequestId));
                    }
                    else
                    {
                        Sender.Tell(new JobExecutionStatus(query.RequestId, JobExecutionStatus.RequestStatus.Running));
                    }

                    break;
            }
        }

        private async Task<JobExecutionStatus> RunJob(JobExecutionRequest request)
        {
            _state = State.Running;
            _cancellationTokenSource = new CancellationTokenSource();
            var chainContext = await _servicePack.ChainContextService.GetChainContextAsync(request.ChainId);

            foreach (var tx in request.Transactions)
            {
                TransactionTrace trace;

                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    trace = new TransactionTrace()
                    {
                        TransactionId = tx.GetHash(),
                        StdErr = "Execution Cancelled"
                    };
                }
                else
                {
                    // TODO: Abort task when cancellation is requested
                    // TODO: Change commit has to be moved to here
                    trace = await ExecuteTransaction(chainContext, tx);
                }

                request.ResultCollector?.Tell(new TransactionTraceMessage(request.RequestId, trace));
            }

            _cancellationTokenSource = null;
            // TODO: What if actor died in the middle

            var retMsg = new JobExecutionStatus(request.RequestId, JobExecutionStatus.RequestStatus.Completed);
            request.ResultCollector?.Tell(retMsg);
            request.Router?.Tell(retMsg);
            _servingRequestId = -1;
            _state = State.Idle;
            return retMsg;
        }

        private async Task<TransactionTrace> ExecuteTransaction(IChainContext chainContext, ITransaction transaction)
        {
            var trace = new TransactionTrace()
            {
                TransactionId = transaction.GetHash()
            };

            var txCtxt = new TransactionContext()
            {
                PreviousBlockHash = chainContext.BlockHash,
                Transaction = transaction,
                Trace = trace
            };

            IExecutive executive = null;

            try
            {
                executive = await _servicePack.SmartContractService
                    .GetExecutiveAsync(transaction.To, chainContext.ChainId);

                await executive.SetTransactionContext(txCtxt).Apply(true);
                trace.Logs.AddRange(txCtxt.Trace.FlattenedLogs);
                // TODO: Check run results / logs etc.
            }
            catch (Exception ex)
            {
                // TODO: Improve log
                txCtxt.Trace.StdErr += ex + "\n";
            }
            finally
            {
                if (executive != null)
                {
                    await _servicePack.SmartContractService.PutExecutiveAsync(transaction.To, executive);
                }
            }

            return trace;
        }
    }
}