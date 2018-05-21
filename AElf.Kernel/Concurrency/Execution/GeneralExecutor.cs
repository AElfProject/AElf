using System;
using System.Collections.Generic;
using Akka.Actor;
using AElf.Kernel.Extensions;
using AElf.Kernel.Services;
using AElf.Kernel.Concurrency.Execution.Messages;
using Google.Protobuf;

namespace AElf.Kernel.Concurrency.Execution
{
	/// <summary>
	/// Manages all chain executors.
	/// </summary>
	public class GeneralExecutor : UntypedActor
	{
		private readonly ActorSystem _system;
		private readonly IChainContextService _chainContextService;
		private readonly IAccountContextService _accountContextService;
		private Dictionary<Hash, IActorRef> _executorByChainId = new Dictionary<Hash, IActorRef>();

		public GeneralExecutor(ActorSystem system, IChainContextService chainContextService, IAccountContextService accountContextService)
		{
			_system = system;
			_chainContextService = chainContextService;
			_accountContextService = accountContextService;
		}

		protected override void OnReceive(object message)
		{
			switch (message)
			{
				case RequestAddChainExecutor req:
					if (!_executorByChainId.TryGetValue(req.ChainId, out var actor))
					{
						// TODO: Handle chainId not found in chain context service
						// TODO: Don't need prefix "0x" if Hash.Zero's string representation is not empty
						actor = Context.ActorOf(ChainExecutor.Props(_chainContextService.GetChainContext(req.ChainId), _accountContextService), "0x" + req.ChainId.ToByteArray().ToHex());
						_executorByChainId.Add(req.ChainId, actor);
					}
					Sender.Tell(new RespondAddChainExecutor(req.ChainId, actor));
					break;
				case RequestGetChainExecutor req:
					_executorByChainId.TryGetValue(req.ChainId, out var a);
					Sender.Tell(new RespondGetChainExecutor(req.ChainId, a));
					break;
				case RequestRemoveChainExecutor req:
					if (_executorByChainId.TryGetValue(req.ChainId, out var a1))
					{
						Context.Stop(a1);
						_executorByChainId.Remove(req.ChainId);
						Sender.Tell(new RespondRemoveChainExecutor(req.ChainId, RespondRemoveChainExecutor.RemoveStatus.Removed));
					}
					else
					{
						Sender.Tell(new RespondRemoveChainExecutor(req.ChainId, RespondRemoveChainExecutor.RemoveStatus.NotExisting));
					}
					break;
					// TODO: Handle children death
			}
		}

		public static Props Props(ActorSystem system, IChainContextService chainContextService, IAccountContextService accountContextService)
		{
			return Akka.Actor.Props.Create(() => new GeneralExecutor(system, chainContextService, accountContextService));
		}
	}
}
