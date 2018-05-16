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
		private ITransaction _transaction;
		private IActorRef _resultCollector;

		public ParallelExecutionTransactionExecutor(IChainContext chainContext, ITransaction transaction, IActorRef resultCollector)
		{
			_chainContext = chainContext;
			_transaction = transaction;
			_resultCollector = resultCollector;
		}

		protected override void OnReceive(object message)
		{
			switch (message)
			{
				case StartExecutionMessage start:
					ExecuteTransaction().ContinueWith(
						task => new TransactionResultMessage(task.Result),
						TaskContinuationOptions.AttachedToParent & TaskContinuationOptions.ExecuteSynchronously
					).PipeTo(Self);
					break;
				case TransactionResultMessage transactionResult:
					if (_resultCollector != null)
					{
						_resultCollector.Tell(transactionResult);
					}
					Context.Stop(Self);
					break;
					// TODO: More messages
			}
		}

		private async Task<TransactionResult> ExecuteTransaction()
		{
			ISmartContractZero smartContractZero = _chainContext.SmartContractZero;
			TransactionResult result = new TransactionResult();
			result.TransactionId = _transaction.GetHash();
			// TODO: Reject tx if IncrementId != Nonce

			try
			{
				await smartContractZero.InvokeAsync(caller: _transaction.From, methodname: _transaction.MethodName, bytes: _transaction.Params);
				result.Status = Status.Mined;
			}
			catch
			{
				result.Status = Status.ExecutedFailed;
			}

			return result;
		}

		public static Props Props(IChainContext chainContext, ITransaction transaction, IActorRef resultCollector)
		{
			return Akka.Actor.Props.Create(() => new ParallelExecutionTransactionExecutor(chainContext, transaction, resultCollector));
		}

	}
}
