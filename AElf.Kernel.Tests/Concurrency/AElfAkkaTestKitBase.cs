using Akka.Actor;
using Akka.Configuration;
using Akka.Remote;
using Akka.TestKit;

namespace AElf.Kernel.Tests.Concurrency
{
    public abstract class AElfAkkaTestKitBase : TestKitBase
    {
        protected AElfKernelIntegratedTest _aelfKernelIntegratedTest=new AElfKernelIntegratedTest();

        protected AElfAkkaTestKitBase(ITestKitAssertions assertions, ActorSystem system = null, string testActorName = null) : base(assertions, system, testActorName)
        {
        }

        protected AElfAkkaTestKitBase(ITestKitAssertions assertions, Config config, string actorSystemName = null, string testActorName = null) : base(assertions, config, actorSystemName, testActorName)
        {
        }
        
        
    }
}