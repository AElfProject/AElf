using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.SmartContractExecution.Execution;
using Akka.Actor;
using AkkaAssembly = Akka;
using Akka.Routing;

namespace AElf.Kernel.SmartContractExecution.Akka.Infrastructure
{
    class TaskNotCompletedProperlyException : Exception
    {
        public TaskNotCompletedProperlyException(string message) : base(message)
        {
        }
    }

    public class Requestor : UntypedActor
    {
        private long _currentRequestId;

        private long NextRequestId => Interlocked.Increment(ref _currentRequestId);

        private Dictionary<long, TaskCompletionSource<List<TransactionTrace>>> _requestIdToTaskCompleteSource =
            new Dictionary<long, TaskCompletionSource<List<TransactionTrace>>>();

        private Dictionary<long, List<TransactionTrace>> _requestIdToTraces =
            new Dictionary<long, List<TransactionTrace>>();

        private Dictionary<long, int> _requesteIdTransactionCounts = new Dictionary<long, int>();
//        private Dictionary<long, HashSet<Hash>> _requestIdToPendingTransactionIds = new Dictionary<long, HashSet<Hash>>();
        
        private IActorRef _router;

        public Requestor(IActorRef router)
        {
            _router = router;
        }

        protected override void OnReceive(object message)
        {
            switch (message)
            {
                case JobExecutionCancelMessage r:
                    _router.Tell(new Broadcast(r));
                    break;
                case LocalExecuteTransactionsMessage req:
                    var reqId = NextRequestId;
                    _requestIdToTaskCompleteSource.Add(reqId, req.TaskCompletionSource);
                    _requestIdToTraces.Add(reqId, new List<TransactionTrace>());
                    _requesteIdTransactionCounts.Add(reqId, req.Transactions.Count);
                    var hashes = new HashSet<Hash>();
                    foreach (var tx in req.Transactions)
                    {
                        hashes.Add(tx.GetHash());
                    }

//                    _requestIdToPendingTransactionIds.Add(reqId, hashes);
                    _router.Tell(new JobExecutionRequest(reqId, req.ChainId, req.Transactions, Self, _router, req.CurrentBlockTime,
                        req.DisambiguationHash, req.SkipFee));
                    break;
                case TransactionTraceMessage msg:
                    if (!_requestIdToTraces.TryGetValue(msg.RequestId, out var traces))
                    {
                        Console.WriteLine("###Debug:{0}, {1}",  msg.RequestId, _requestIdToTraces.Keys);
                        throw new TaskNotCompletedProperlyException("TransactionTrace is received after the task has completed.");
                    }

                    traces = new List<TransactionTrace>(msg.TransactionTraces);
//                    _requestIdToPendingTransactionIds[msg.RequestId].Remove(msg.TransactionTrace.TransactionId);
                    if (traces.Count == _requesteIdTransactionCounts[msg.RequestId])
                    {
                        _requestIdToTaskCompleteSource[msg.RequestId].TrySetResult(traces);
                        _requesteIdTransactionCounts.Remove(msg.RequestId);
                        _requestIdToTaskCompleteSource.Remove(msg.RequestId);
                        _requestIdToTraces.Remove(msg.RequestId);
                    }
                    break;
                case JobExecutionStatus status:
                    HandleExecutionStatus(status);
                    break;
            }
        }

        private void HandleExecutionStatus(JobExecutionStatus status)
        {
            if (status.Status != JobExecutionStatus.RequestStatus.Running &&
                status.Status != JobExecutionStatus.RequestStatus.Completed)
            {
                _requestIdToTaskCompleteSource[status.RequestId].TrySetResult(new List<TransactionTrace>());
                _requesteIdTransactionCounts.Remove(status.RequestId);
                _requestIdToTaskCompleteSource.Remove(status.RequestId);
                _requestIdToTraces.Remove(status.RequestId);
//                _requestIdToPendingTransactionIds.Remove(status.RequestId);
            }
        }
        
        public static Props Props(IActorRef router)
        {
            return AkkaAssembly.Actor.Props.Create(() => new Requestor(router));
        }
    }
}