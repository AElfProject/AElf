using AElf.Kernel;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Contracts.DPoS.Tests
{
    [UseAutofacTestFramework]
    // ReSharper disable once InconsistentNaming
    public class DPoSTest
    {
        private TestDPoSContractShim _contractShim;
        public DPoSTest(TestDPoSContractShim contractShim)
        {
            _contractShim = contractShim;
        }
        
        [Fact]
        public void BlockProducderTest()
        {
            var ts = _contractShim.SetBlockProducers();
            Assert.NotNull(ts);
        }
    }
}