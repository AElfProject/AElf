using System.Threading.Tasks;
using AElf.Kernel.Services;
using Xunit.Frameworks.Autofac;

namespace AElf.Runtime.CSharp.Tests
{
    [UseAutofacTestFramework]
    public class DPoSTest
    {
        private MockSetup _mock;
        private ISmartContractService _service;
        public DPoSTest(MockSetup mock)
        {
            _mock = mock;
            _service = mock.SmartContractService;
        }

        public async Task FirstTwoRoundsOrderTest()
        {
            
        }
    }
}