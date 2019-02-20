//using System.Collections.Generic;
//using System.Threading.Tasks;
//using AElf.ChainController;
//using Google.Protobuf;
//using Xunit;
//using AElf.Common;
//using AElf.TestBase;
//using Shouldly;

namespace AElf.Kernel.Tests
{
    /*
    public class ChainCreationServiceTests : AElfKernelTestBase
    {
        private readonly IChainCreationService _service;

        public ChainCreationServiceTests()
        {
            _service = this.GetRequiredService<IChainCreationService>();
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
                ContractHash = Hash.FromRawBytes(SmartContractZeroCode)
            };
            var chain = await _service.CreateNewChainAsync(ChainHelpers.GetChainId(123), new List<SmartContractRegistration>{reg});
            Assert.Equal(ChainHelpers.GetChainId(123), chain.Id);
        }
    }
    */
}