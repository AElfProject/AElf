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
		private IActorRef _generalExecutor;
        private MockSetup _mock;
        private ActorSystem sys = ActorSystem.Create("test");
        private IActorRef _serviceRouter;

        public GeneralExecutorTest(MockSetup mock) : base(new XunitAssertions())
        {
            _mock = mock;
            _serviceRouter = sys.ActorOf(LocalServicesProvider.Props(_mock.ServicePack));
            _generalExecutor = sys.ActorOf(GeneralExecutor.Props(sys, _serviceRouter), "exec");
		}

		[Fact]
		public void Test(){
            TestWithChainId(_mock.ChainId1);
            TestWithChainId(_mock.ChainId2);
		}

		private void TestWithChainId(Hash chainId)
		{
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
