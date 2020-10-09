using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace AElf.ContractTestKit.AEDPoSExtension
{
    public class ConsensusFactDiscoverer : FactDiscoverer
    {
        private readonly IChainTypeProvider _chainTypeProvider;
        private readonly IMessageSink _diagnosticMessageSink;

        public ConsensusFactDiscoverer(IMessageSink diagnosticMessageSink, IChainTypeProvider chainTypeProvider) : base(
            diagnosticMessageSink)
        {
            _chainTypeProvider = chainTypeProvider;
            _diagnosticMessageSink = diagnosticMessageSink;
        }

        public override IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions,
            ITestMethod testMethod, IAttributeInfo factAttribute)
        {
            var isSideChain = factAttribute.GetNamedArgument<bool>("IsSideChain");
            _chainTypeProvider.IsSideChain = isSideChain;
            yield return new ConsensusTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(),
                testMethod, isSideChain);
        }
    }
}