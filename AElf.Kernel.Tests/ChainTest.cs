using System;
using System.Threading.Tasks;
using AElf.Kernel.KernelAccount;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class ChainTest
    {
        private readonly IChainCreationService _chainCreationService;
        private readonly ISmartContractZero _smartContractZero;

        public ChainTest(ISmartContractZero smartContractZero, IChainCreationService chainCreationService)
        {
            _smartContractZero = smartContractZero;
            _chainCreationService = chainCreationService;
        }

        [Fact]
        public async Task CreateChain()
        {
            var chain = await _chainCreationService.CreateNewChainAsync(Hash.Generate(), _smartContractZero);
            Assert.Equal(1, chain.CurrentBlockHeight);
            //Console.WriteLine(_smartContractZero.GetHash().Value.ToByteArray().ToString());
        }
    }
}