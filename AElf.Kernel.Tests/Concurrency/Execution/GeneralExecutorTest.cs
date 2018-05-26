using System;
using System.Linq;
using System.Collections.Generic;
using Xunit;
using Xunit.Frameworks.Autofac;
using Akka.Actor;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using AElf.Kernel.Extensions;
using AElf.Kernel.Concurrency.Execution;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.Services;
using Google.Protobuf;

namespace AElf.Kernel.Tests.Concurrency.Execution
{
	[UseAutofacTestFramework]
	public class GeneralExecutorTest : TestKitBase
	{
		private ActorSystem sys = ActorSystem.Create("test");
		private ChainContextWithSmartContractZeroWithTransfer _chainContext;
		private ChainContextServiceWithAdd _chainContextService;
		private IAccountContextService _accountContextService;
		private IActorRef _generalExecutor;

		public GeneralExecutorTest(ChainContextServiceWithAdd chainContextService, AccountContextService accountContextService, ChainContextWithSmartContractZeroWithTransfer chainContext) : base(new XunitAssertions())
		{
			_chainContextService = chainContextService;
			_accountContextService = accountContextService;
			_chainContext = chainContext;
			_generalExecutor = sys.ActorOf(GeneralExecutor.Props(sys, _chainContextService, _accountContextService), "exec");
		}

		[Fact]
		public void Test(){
			TestWithChainId(Hash.Zero);
			TestWithChainId(Hash.Generate());
		}

		private void TestWithChainId(Hash chainId)
		{
			_chainContextService.AddChainContext(chainId, _chainContext);

			// Add the chain executor
			_generalExecutor.Tell(new RequestAddChainExecutor(chainId));
			var add = ExpectMsg<RespondAddChainExecutor>();
			Assert.Equal(chainId, add.ChainId);

            // Get the same chain executor
			_generalExecutor.Tell(new RequestGetChainExecutor(chainId));
            var get = ExpectMsg<RespondGetChainExecutor>();
            Assert.Equal(chainId, get.ChainId);
			Assert.Equal(add.ActorRef, get.ActorRef);

            // Remove a NotExisting chain executor
			Hash notExistingChainId = Hash.Generate();
			_generalExecutor.Tell(new RequestRemoveChainExecutor(notExistingChainId));
			var removeNotExisting = ExpectMsg<RespondRemoveChainExecutor>();
			Assert.Equal(notExistingChainId, removeNotExisting.ChainId);
			Assert.Equal(RespondRemoveChainExecutor.RemoveStatus.NotExisting, removeNotExisting.Status);

            // Remove added chain executor
			_generalExecutor.Tell(new RequestRemoveChainExecutor(chainId));
			var remove = ExpectMsg<RespondRemoveChainExecutor>();
			Assert.Equal(chainId, remove.ChainId);
			Assert.Equal(RespondRemoveChainExecutor.RemoveStatus.Removed, remove.Status);

            // Get chain executor returns null
			_generalExecutor.Tell(new RequestGetChainExecutor(chainId));
			var getNull = ExpectMsg<RespondGetChainExecutor>();
			Assert.Equal(chainId, getNull.ChainId);
			Assert.Null(getNull.ActorRef);
		}
	}
}
