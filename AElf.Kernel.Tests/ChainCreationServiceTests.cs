using System.Threading.Tasks;
using AElf.Services;
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
        
        public byte[] SmartContractZeroCode
        {
            get
            {
                return ContractCodes.TestContractZeroCode;
            }
        }

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
            var chain = await _service.CreateNewChainAsync("Hello".CalculateHash(), reg);
            Assert.Equal("Hello".CalculateHash(), chain.Id);
        }
    }
}