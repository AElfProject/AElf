using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Akka.Actor;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.Extensions;
using Google.Protobuf;
namespace AElf.Kernel.Concurrency.Execution
{
	/// <summary>
	/// Supervises Chain Requestors and forward messages to them according to ChainId.
	/// </summary>
	public class GeneralRequestor : UntypedActor
	{
		private readonly ActorSystem _system;
		private Dictionary<Hash, IActorRef> _requestorByChainId = new Dictionary<Hash, IActorRef>();

		public GeneralRequestor(ActorSystem system)
		{
			_system = system;
		}

		protected override void OnReceive(object message)
		{
			switch (message)
			{
				case LocalExecuteTransactionsMessage req:
					GetRequestor(req.ChainId).Forward(req);
					break;
					// TODO: Handle children death
			}
		}

		private IActorRef GetRequestor(Hash chainId)
		{
			if (!_requestorByChainId.TryGetValue(chainId, out var actor))
			{
				actor = Context.ActorOf(ChainRequestor.Props(_system, chainId), "0x" + chainId.ToByteArray().ToHex());
				_requestorByChainId.Add(chainId, actor);
			}
			return actor;
		}      

        public static Props Props(ActorSystem system)
        {
			return Akka.Actor.Props.Create(() => new GeneralRequestor(system));
        }
	}
}
