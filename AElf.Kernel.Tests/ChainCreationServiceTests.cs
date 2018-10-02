using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common.Extensions;
using Google.Protobuf;
using Xunit;
using Xunit.Frameworks.Autofac;

namespace AElf.Kernel.Tests
{
    [UseAutofacTestFramework]
    public class ChainCreationServiceTests
    {
        private IChainCreationService _service;

        public ChainCreationServiceTests(IChainCreationService service)
        {
            _service = service;
        }
        
        private byte[] SmartContractZeroCode => ContractCodes.TestContractZeroCode;

        [Fact]
        public async Task Test()
        {
            // TODO: *** Contract Issues ***
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = Hash.Zero
            };
            var chain = await _service.CreateNewChainAsync("Hello".CalculateHash(), new List<SmartContractRegistration>{reg});
            Assert.Equal("Hello".CalculateHash().ToHex(), chain.Id.Dumps());
        }
    }
}