using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.SmartContractExecution.Application;
using Akka.Actor;
using Address = AElf.Common.Address;

namespace AElf.Kernel.SmartContractExecution.Execution
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
        public RequestExecuteTransactions(long requestId, List<Transaction> transactions)
        {
            RequestId = requestId;
            Transactions = transactions;
        }

        public long RequestId { get; }
        public List<Transaction> Transactions { get; }
    }

    public sealed class RespondExecuteTransactions
    {
        public enum RequestStatus
        {
            Rejected,
            Executed
        }

        public RespondExecuteTransactions(long requestId, RequestStatus status,
            List<TransactionTrace> transactionTraces)
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
        public RequestAddChainExecutor(int chainId)
        {
            ChainId = chainId;
        }

        public int ChainId { get; }
    }

    public sealed class RespondAddChainExecutor
    {
        public RespondAddChainExecutor(int chainId, IActorRef actorRef)
        {
            ChainId = chainId;
            ActorRef = actorRef;
        }

        public int ChainId { get; }
        public IActorRef ActorRef { get; }
    }


    public sealed class RequestGetChainExecutor
    {
        public RequestGetChainExecutor(int chainId)
        {
            ChainId = chainId;
        }

        public int ChainId { get; }
    }

    public sealed class RespondGetChainExecutor
    {
        public RespondGetChainExecutor(int chainId, IActorRef actorRef)
        {
            ChainId = chainId;
            ActorRef = actorRef;
        }

        public int ChainId { get; }
        public IActorRef ActorRef { get; }
    }

    public sealed class RequestRemoveChainExecutor
    {
        public RequestRemoveChainExecutor(int chainId)
        {
            ChainId = chainId;
        }

        public int ChainId { get; }
    }

    public sealed class RespondRemoveChainExecutor
    {
        public enum RemoveStatus
        {
            NotExisting,
            Removed
        }

        public RespondRemoveChainExecutor(int chainId, RemoveStatus status)
        {
            ChainId = chainId;
            Status = status;
        }

        public int ChainId { get; }
        public RemoveStatus Status { get; }
    }

    #endregion Chain Executors

    /// <summary>
    /// Message sent to local requestor for transaction execution.
    /// </summary>
    public sealed class LocalExecuteTransactionsMessage
    {
        public LocalExecuteTransactionsMessage(int chainId, List<Transaction> transactions,
            TaskCompletionSource<List<TransactionTrace>> taskCompletionSource, DateTime currentBlockTime,
            Hash disambiguationHash = null, bool skipFee = false)
        {
            ChainId = chainId;
            Transactions = transactions;
            TaskCompletionSource = taskCompletionSource;
            CurrentBlockTime = currentBlockTime;
            DisambiguationHash = disambiguationHash;

            SkipFee = skipFee;
        }

        public int ChainId { get; }
        public List<Transaction> Transactions { get; }
        public TaskCompletionSource<List<TransactionTrace>> TaskCompletionSource { get; }
        public DateTime CurrentBlockTime { get; set; }
        public Hash DisambiguationHash { get; }
        public bool SkipFee { get; }
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


    #region Singleton Messages

    /// <summary>
    /// Short-lived executor actors require a <see cref="StartExecutionMessage"/> to start execution.
    /// </summary>
    public sealed class StartExecutionMessage
    {
        private StartExecutionMessage()
        {
        }

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
        private StartGroupingMessage()
        {
        }

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
        private StartBatchingMessage()
        {
        }

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

    public class JobExecutionRequest
    {
        public JobExecutionRequest(long requestId, int chainId, List<Transaction> transactions,
            IActorRef resultCollector, IActorRef router, DateTime currentBlockTime, Hash disambiguationHash = null, 
            bool skipFee = false)
        {
            RequestId = requestId;
            ChainId = chainId;
            Transactions = transactions;
            ResultCollector = resultCollector;
            Router = router;
            CurrentBlockTime = currentBlockTime;
            DisambiguationHash = disambiguationHash;

            SkipFee = skipFee;
        }

        public long RequestId { get; set; }
        public int ChainId { get; set; }
        public Hash DisambiguationHash { get; set; }
        public List<Transaction> Transactions { get; set; }
        public IActorRef ResultCollector { get; set; }
        public IActorRef Router { get; set; }
        public DateTime CurrentBlockTime { get; set; }
        public bool SkipFee { get; set; }
    }

    public sealed class JobExecutionCancelMessage
    {
        private JobExecutionCancelMessage()
        {
        }

        /// <summary>
        /// The singleton instance of JobExecutionCancelMessage.
        /// </summary>
        public static JobExecutionCancelMessage Instance { get; } = new JobExecutionCancelMessage();

        /// <inheritdoc/>
        public override string ToString()
        {
            return "<JobExecutionCancelMessage>";
        }
    }

    public sealed class JobExecutionCancelAckMessage
    {
        private JobExecutionCancelAckMessage()
        {
        }

        /// <summary>
        /// The singleton instance of JobExecutionCancelMessage.
        /// </summary>
        public static JobExecutionCancelAckMessage Instance { get; } = new JobExecutionCancelAckMessage();

        /// <inheritdoc/>
        public override string ToString()
        {
            return "<JobExecutionCancelAckMessage>";
        }
    }

    public sealed class JobExecutionStatusQuery
    {
        public JobExecutionStatusQuery(long requestId)
        {
            RequestId = requestId;
        }

        public long RequestId { get; }
    }

    public sealed class JobExecutionStatus
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
    }

    #endregion Routed workers
}