using System;
using System.Threading;
using System.Threading.Tasks;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.SmartContractExecution.Execution;
using Akka.Actor;

/*
    Todo: #338
    There are stability issues about TrackedRouter, so we use the Akka default router temporarily.
    Some of the code is annotated, marked with "todo" and optimized later.
 */

// ReSharper disable once CheckNamespace
namespace AElf.Kernel.SmartContractExecution
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
        // private long _servingRequestId = -1;
        private ServicePack _servicePack;
        //private IExecutingService _proxyExecutingService;

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
                        /*_proxyExecutingService = new SimpleExecutingService(_servicePack.SmartContractService,
                            _servicePack.TransactionTraceManager, _servicePack.StateManager,
                            _servicePack.ChainContextService);*/
                        _state = State.Idle;
                    }

                    break;
                case JobExecutionRequest req:
                    if (_state == State.Idle)
                    {
                        _cancellationTokenSource?.Dispose();
                        _cancellationTokenSource = new CancellationTokenSource();

                        var self = Self;
                        Task.Run(() =>
                        {
                            RunJob(req)
                                .ContinueWith(
                                    task => task.Result,
                                    TaskContinuationOptions.AttachedToParent & TaskContinuationOptions.ExecuteSynchronously
                                ).PipeTo(self);
                        });
                        
/*
 Temporarily disabled.
 TODO: https://github.com/AElfProject/AElf/issues/338
                        Sender.Tell(new JobExecutionStatus(req.RequestId, JobExecutionStatus.RequestStatus.Running));
*/
                    }
/*
 Temporarily disabled.
 TODO: https://github.com/AElfProject/AElf/issues/338
                    else if (_state == State.PendingSetSericePack)
                    {
                        Sender.Tell(new JobExecutionStatus(req.RequestId,
                            JobExecutionStatus.RequestStatus.FailedDueToWorkerNotReady));
                    }
                    else
                    {
                        Sender.Tell(new JobExecutionStatus(req.RequestId, JobExecutionStatus.RequestStatus.Rejected));
                    }
*/
                    break;

                case JobExecutionCancelMessage c:
                    _cancellationTokenSource?.Cancel();
                    Sender.Tell(JobExecutionCancelAckMessage.Instance);
                    break;

/*
 Temporarily disabled.
 TODO: https://github.com/AElfProject/AElf/issues/338
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
*/
            }
        }

        private async Task<JobExecutionStatus> RunJob(JobExecutionRequest request)
        {
            throw new NotImplementedException();
///*
// Temporarily disabled.
// TODO: https://github.com/AElfProject/AElf/issues/338
//            _state = State.Running;
//*/
//            var result = await _proxyExecutingService.ExecuteAsync(request.Transactions, request.ChainId,
//                request.CurrentBlockTime, _cancellationTokenSource.Token, request.DisambiguationHash,
//                request.TransactionType, request.SkipFee);
//            request.ResultCollector?.Tell(new TransactionTraceMessage(request.RequestId, result));
//
//            // TODO: What if actor died in the middle
//
//            var retMsg = new JobExecutionStatus(request.RequestId, JobExecutionStatus.RequestStatus.Completed);
//            // TODO: tell requestor and router about the worker complete job,and set to idle state.
///*
// Temporarily disabled.
// TODO: https://github.com/AElfProject/AElf/issues/338
//            request.ResultCollector?.Tell(retMsg);
//            request.Router?.Tell(retMsg);
//
//            _servingRequestId = -1;
//            _state = State.Idle;
//*/
//            return retMsg;
        }
    }
}