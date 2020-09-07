using System;
using Xunit;
using Xunit.Sdk;

namespace AElf.ContractTestKit.AEDPoSExtension
{
    [XunitTestCaseDiscoverer("AElf.ContractTestKit.AEDPoSExtension.ConsensusFactDiscoverer",
        "AElf.ContractTestKit.AEDPoSExtension")]
    [AttributeUsage(AttributeTargets.Method)]
    public class ConsensusFactAttribute : FactAttribute
    {
        public virtual bool IsSideChain { get; set; }
    }
}