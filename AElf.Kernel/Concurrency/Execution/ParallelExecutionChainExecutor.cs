using System;
using System.Collections.Generic;
using Akka.Actor;
using AElf.Kernel.Services;
using AElf.Kernel.Concurrency.Execution.Messages;

namespace AElf.Kernel.Concurrency.Execution
{
	public class ParallelExecutionChainExecutor : UntypedActor
	{
		private IChainContext _chainContext;
		private IAccountContextService _accountContextService;

		public ParallelExecutionChainExecutor(IChainContext chainContext, IAccountContextService accountContextService)
		{
			_chainContext = chainContext;
			_accountContextService = accountContextService;
		}

		protected override void OnReceive(object message)
		{
			switch (message)
			{
				case RequestAccountDataContext req:
					var accountDataContext = _accountContextService.GetAccountDataContext(req.AccountHash, _chainContext.ChainId);
					Sender.Tell(new RespondAccountDataContext(req.RequestId, accountDataContext));
					break;
					// TODO: More messages
			}
		}

		public static Props Props(IChainContext chainContext, IAccountContextService accountContextService)
		{
			return Akka.Actor.Props.Create(() => new ParallelExecutionChainExecutor(chainContext, accountContextService));
		}

	}
}
