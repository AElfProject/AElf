using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Types;
using Akka.Actor;
using Akka.Cluster;
using Akka.Routing;

namespace AElf.Kernel.Concurrency.Execution.Messages
{
    #region LocalServices
    public sealed class RequestLocalSerivcePack
    {
        public RequestLocalSerivcePack(long requestId)
        {
            RequestId = requestId;
        }

        public long RequestId { get; }
    }

    public sealed class LocalSerivcePack
    {
        public LocalSerivcePack(ServicePack servicePack)
        {
            ServicePack = servicePack;
        }

        public ServicePack ServicePack { get; }
    }
    #endregion LocalServices

    #region ExecuteTransactions
    public sealed class RequestExecuteTransactions
    {
        public RequestExecuteTransactions(long requestId, List<ITransaction> transactions)
        {
            RequestId = requestId;
            Transactions = transactions;
        }

        public long RequestId { get; }
        public List<ITransaction> Transactions { get; }
    }

    public sealed class RespondExecuteTransactions
    {
        public enum RequestStatus
        {
            Rejected,
            Executed
        }
        public RespondExecuteTransactions(long requestId, RequestStatus status, List<TransactionTrace> transactionTraces)
        {
            RequestId = requestId;
            Status = status;
            TransactionTraces = transactionTraces;
        }

        public long RequestId { get; }
        public RequestStatus Status { get; }
        public List<TransactionTrace> TransactionTraces { get; }
    }
    #endregion ExecuteTransactions

    #region Chain Executors
    public sealed class RequestAddChainExecutor
    {
        public RequestAddChainExecutor(Hash chainId)
        {
            ChainId = chainId;
        }
        public Hash ChainId { get; }
    }

    public sealed class RespondAddChainExecutor
    {
        public RespondAddChainExecutor(Hash chainId, IActorRef actorRef)
        {
            ChainId = chainId;
            ActorRef = actorRef;
        }
        public Hash ChainId { get; }
        public IActorRef ActorRef { get; }
    }


    public sealed class RequestGetChainExecutor
    {
        public RequestGetChainExecutor(Hash chainId)
        {
            ChainId = chainId;
        }
        public Hash ChainId { get; }
    }

    public sealed class RespondGetChainExecutor
    {
        public RespondGetChainExecutor(Hash chainId, IActorRef actorRef)
        {
            ChainId = chainId;
            ActorRef = actorRef;
        }
        public Hash ChainId { get; }
        public IActorRef ActorRef { get; }
    }

    public sealed class RequestRemoveChainExecutor
    {
        public RequestRemoveChainExecutor(Hash chainId)
        {
            ChainId = chainId;
        }
        public Hash ChainId { get; }
    }

    public sealed class RespondRemoveChainExecutor
    {
        public enum RemoveStatus
        {
            NotExisting,
            Removed
        }
        public RespondRemoveChainExecutor(Hash chainId, RemoveStatus status)
        {
            ChainId = chainId;
            Status = status;
        }
        public Hash ChainId { get; }
        public RemoveStatus Status { get; }
    }
    #endregion Chain Executors

    /// <summary>
    /// Message sent to local requestor for transaction execution.
    /// </summary>
    public sealed class LocalExecuteTransactionsMessage:IConsistentHashable
    {
        public LocalExecuteTransactionsMessage(Hash chainId, List<ITransaction> transactions, TaskCompletionSource<List<TransactionTrace>> taskCompletionSource)
        {
            ChainId = chainId;
            Transactions = transactions;
            TaskCompletionSource = taskCompletionSource;
        }

        public Hash ChainId { get; }
        public List<ITransaction> Transactions { get; }
        public TaskCompletionSource<List<TransactionTrace>> TaskCompletionSource { get; }
        public object ConsistentHashKey { get; }
    }

//    public sealed class TransactionResultMessage
//    {
//        public TransactionResultMessage(TransactionResult transactionResult)
//        {
//            TransactionResult = transactionResult;
//        }
//
//        public TransactionResult TransactionResult { get; }
//    }

    public sealed class TransactionTraceMessage : IConsistentHashable
    {
        public TransactionTraceMessage(long requestId, TransactionTrace transactionTrace)
        {
            RequestId = requestId;
            TransactionTrace = transactionTrace;
            ConsistentHashKey = GetHashCode();
        }

        public long RequestId { get; set; }
        public TransactionTrace TransactionTrace { get; set; }
        public object ConsistentHashKey { get; }
    }

    #region Singleton Messages
    /// <summary>
    /// Short-lived executor actors require a <see cref="StartExecutionMessage"/> to start execution.
    /// </summary>
    public sealed class StartExecutionMessage
    {
        private StartExecutionMessage() { }

        /// <summary>
        /// The singleton instance of StartExecutionMessage.
        /// </summary>
        public static StartExecutionMessage Instance { get; } = new StartExecutionMessage();

        /// <inheritdoc/>
        public override string ToString()
        {
            return "<StartExecutionMessage>";
        }
    }

    /// <summary>
    /// <see cref="StartGroupingMessage"/> is automatically sent to the actor itself upon starting so that grouping will start.
    /// </summary>
    public sealed class StartGroupingMessage
    {
        private StartGroupingMessage() { }

        /// <summary>
        /// The singleton instance of StartGroupingMessage.
        /// </summary>
        public static StartGroupingMessage Instance { get; } = new StartGroupingMessage();

        /// <inheritdoc/>
        public override string ToString()
        {
            return "<StartGroupingMessage>";
        }
    }

    /// <summary>
    /// <see cref="StartBatchingMessage"/> is automatically sent to the actor itself upon starting so that batching will start.
    /// </summary>
    public sealed class StartBatchingMessage
    {
        private StartBatchingMessage() { }

        /// <summary>
        /// The singleton instance of StartBatchingMessage.
        /// </summary>
        public static StartBatchingMessage Instance { get; } = new StartBatchingMessage();

        /// <inheritdoc/>
        public override string ToString()
        {
            return "<StartBatchingMessage>";
        }
    }
    #endregion Singleton Messages

    #region Routed workers

//    public sealed class RoutedJobExecutionRequest
//    {
//        public RoutedJobExecutionRequest(JobExecutionRequest request, IActorRef router)
//        {
//            Request = request;
//            Router = router;
//        }
//
//        public JobExecutionRequest Request { get; }
//        public IActorRef Router { get; }
//    }

    public class JobExecutionRequest:IConsistentHashable
    {
        public JobExecutionRequest(long requestId, Hash chainId, List<ITransaction> transactions, IActorRef resultCollector, IActorRef router)
        {
            RequestId = requestId;
            ChainId = chainId;
            Transactions = transactions;
            ResultCollector = resultCollector;
            Router = router;
            ConsistentHashKey = GetHashCode();
        }

        public long RequestId { get; set; }
        public Hash ChainId { get; set; }
        public List<ITransaction> Transactions { get; set; }
        public IActorRef ResultCollector { get; set; }
        public IActorRef Router { get; set; }

        public object ConsistentHashKey { get; }
    }

    public sealed class JobExecutionCancelMessage:IConsistentHashable
    {
        private JobExecutionCancelMessage() { }

        /// <summary>
        /// The singleton instance of JobExecutionCancelMessage.
        /// </summary>
        public static JobExecutionCancelMessage Instance { get; } = new JobExecutionCancelMessage();

        /// <inheritdoc/>
        public override string ToString()
        {
            return "<JobExecutionCancelMessage>";
        }

        public object ConsistentHashKey { get; }
    }

    public sealed class JobExecutionCancelAckMessage:IConsistentHashable
    {
        private JobExecutionCancelAckMessage() { }

        /// <summary>
        /// The singleton instance of JobExecutionCancelMessage.
        /// </summary>
        public static JobExecutionCancelAckMessage Instance { get; } = new JobExecutionCancelAckMessage();

        /// <inheritdoc/>
        public override string ToString()
        {
            return "<JobExecutionCancelAckMessage>";
        }

        public object ConsistentHashKey { get; }
    }
    
    public sealed class JobExecutionStatusQuery:IConsistentHashable
    {
        public JobExecutionStatusQuery(long requestId)
        {
            RequestId = requestId;
        }

        public long RequestId { get; }
        public object ConsistentHashKey { get; }
    }
    
    public sealed class JobExecutionStatus:IConsistentHashable
    {
        public enum RequestStatus
        {
            FailedDueToNoAvailableWorker,
            FailedDueToWorkerNotReady,
            Running,
            Completed,
            Rejected,
            InvalidRequestId
        }
        
        public JobExecutionStatus(long requestId, RequestStatus status)
        {
            RequestId = requestId;
            Status = status;
        }

        public long RequestId { get; }
        public RequestStatus Status { get; }
        public object ConsistentHashKey { get; }
    }

    #endregion Routed workers

}