using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Concurrency.Execution.Messages
{
	#region AccountDataContext
	public sealed class RequestAccountDataContext
	{
		public RequestAccountDataContext(long requestId, Hash hash)
		{
			RequestId = requestId;
			AccountHash = hash;
		}

		public long RequestId { get; }
		public Hash AccountHash { get; }
	}

	public sealed class RespondAccountDataContext
	{
		public RespondAccountDataContext(long requestId, IAccountDataContext accountDataContext)
		{
			RequestId = requestId;
			AccountDataContext = accountDataContext;
		}

		public long RequestId { get; }
		public IAccountDataContext AccountDataContext { get; }
	}
	#endregion AccountDataContext

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
		public RespondExecuteTransactions(long requestId, RequestStatus status, List<TransactionResult> transactionResults)
		{
			RequestId = requestId;
			Status = status;
			TransactionResults = transactionResults;
		}

		public long RequestId { get; }
		public RequestStatus Status { get; }
		public List<TransactionResult> TransactionResults { get; }
	}
	#endregion ExecuteTransactions

    /// <summary>
    /// Message sent to local requestor for transaction execution.
    /// </summary>
	public sealed class LocalExecuteTransactionsMessage
	{
		public LocalExecuteTransactionsMessage(Hash chainId, List<Transaction> transactions, TaskCompletionSource<bool> taskCompletionSource)
		{
			ChainId = chainId;
			Transactions = transactions;
			TaskCompletionSource = taskCompletionSource;
		}

		public Hash ChainId { get; }
		public List<Transaction> Transactions { get; }
		public TaskCompletionSource<bool> TaskCompletionSource { get; }
	}

	public sealed class TransactionResultMessage
	{
		public TransactionResultMessage(TransactionResult transactionResult)
		{
			TransactionResult = transactionResult;
		}

		public TransactionResult TransactionResult { get; }
	}

	public sealed class StartExecutionMessage { }
	public sealed class StartGroupingMessage { }
	public sealed class StartBatchingMessage { }
}