using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ChainController;
using Google.Protobuf;
using Xunit;
using AElf.Common;
using AElf.TestBase;

namespace AElf.Kernel.Tests
{
    public class ChainCreationServiceTests : AElfKernelIntegratedTest
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
            var chain = await _service.CreateNewChainAsync(Hash.LoadByteArray(new byte[] { 0x01, 0x02, 0x03 }), new List<SmartContractRegistration>{reg});
            Assert.Equal(Hash.LoadByteArray(new byte[] { 0x01, 0x02, 0x03 }).DumpBase58(), chain.Id.DumpBase58());
        }
    }
}