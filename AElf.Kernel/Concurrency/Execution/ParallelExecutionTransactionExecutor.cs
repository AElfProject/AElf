using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.KernelAccount;

namespace AElf.Kernel.Concurrency.Execution
{
	public class ParallelExecutionTransactionExecutor : UntypedActor
	{
		private IChainContext _chainContext;
		public ParallelExecutionTransactionExecutor(IChainContext chainContext)
		{
			_chainContext = chainContext;
		}

		protected override void OnReceive(object message)
		{
			switch (message)
			{
				case RequestTransactionExecution req:
					var origSender = Sender;
					ExecuteTransaction(req.Transaction).ContinueWith(
						task => new RespondTransactionExecution(req.RequestId, task.Result),
						TaskContinuationOptions.AttachedToParent & TaskContinuationOptions.ExecuteSynchronously
					).PipeTo(origSender);
					break;
					// TODO: More messages
			}
		}

		private async Task<TransactionResult> ExecuteTransaction(ITransaction tx)
		{
			ISmartContractZero smartContractZero = _chainContext.SmartContractZero;
			TransactionResult result = new TransactionResult();
			result.TransactionId = tx.GetHash();
			// TODO: Reject tx if IncrementId != Nonce
         
			try
			{
				await smartContractZero.InvokeAsync(caller: tx.From, methodname: tx.MethodName, bytes: tx.Params);
				result.Status = Status.Mined;
			}
			catch
			{
				result.Status = Status.ExecutedFailed;
			}

			return result;
		}

		public static Props Props(IChainContext chainContext)
		{
			return Akka.Actor.Props.Create(() => new ParallelExecutionTransactionExecutor(chainContext));
		}

	}
}
