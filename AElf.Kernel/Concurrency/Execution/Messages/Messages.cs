using System;

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

	#region TransactionExecution
	public sealed class RequestTransactionExecution
	{
		public RequestTransactionExecution(long requestId, ITransaction transaction)
		{
			RequestId = requestId;
			Transaction = transaction;
		}

		public long RequestId { get; }
		public ITransaction Transaction { get; }
	}

	public sealed class RespondTransactionExecution
    {
		public RespondTransactionExecution(long requestId, TransactionResult transactionResult)
        {
            RequestId = requestId;
			TransactionResult = transactionResult;
        }

        public long RequestId { get; }
		public TransactionResult TransactionResult { get; }
    }
	#endregion TransactionExecution
}