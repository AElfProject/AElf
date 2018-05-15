using Xunit;
using Xunit.Frameworks.Autofac;
using Akka.Actor;
using Akka.TestKit;
using Akka.TestKit.Xunit;
using AElf.Kernel.Concurrency.Execution;
using AElf.Kernel.Concurrency.Execution.Messages;
using AElf.Kernel.Services;

namespace AElf.Kernel.Tests.Concurrency.Execution
{
	[UseAutofacTestFramework]
	public class ParallelExecutionChainExecutorTests : TestKitBase
	{
		private ActorSystem sys = ActorSystem.Create("test");
		private ChainContext _chainContext;
		private AccountContextService _accountContextService;

		public ParallelExecutionChainExecutorTests(ChainContext chainContext, AccountContextService accountContextService) : base(new XunitAssertions())
		{
			_chainContext = chainContext;
			_accountContextService = accountContextService;
		}

		[Fact]
		public void RequestAccountDataContextTest()
		{
			Hash accountHash = Hash.Generate();
			var chainExecutor = sys.ActorOf(ParallelExecutionChainExecutor.Props(_chainContext, _accountContextService));

			chainExecutor.Tell(new RequestAccountDataContext(42, accountHash));
			var accountDataContext = ExpectMsg<RespondAccountDataContext>();
			Assert.Equal(42, accountDataContext.RequestId);
			Assert.Equal(accountHash, accountDataContext.AccountDataContext.Address);
			Assert.Equal((ulong)0, accountDataContext.AccountDataContext.IncreasementId);

			var localAccountDataContext = _accountContextService.GetAccountDataContext(accountHash, _chainContext.ChainId);
			localAccountDataContext.IncreasementId += 1;

			chainExecutor.Tell(new RequestAccountDataContext(43, accountHash));
            accountDataContext = ExpectMsg<RespondAccountDataContext>();
            Assert.Equal((ulong)1, accountDataContext.AccountDataContext.IncreasementId);        
		}

	}
}