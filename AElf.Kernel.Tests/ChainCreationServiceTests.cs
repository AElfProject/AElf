using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ChainController;
using Google.Protobuf;
using Xunit;
using AElf.Common;
using AElf.Kernel.Storages;
using AElf.TestBase;
using Shouldly;

namespace AElf.Kernel.Tests
{
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

            var n = (int) Hash.LoadByteArray(new byte[] {0x01, 0x02, 0x03});
            n.ToStorageKey().ShouldBe(new byte[] {0x00, 0x01, 0x02, 0x03 }.ToHex());
            
            // TODO: *** Contract Issues ***
            var reg = new SmartContractRegistration
            {
                Category = 0,
                ContractBytes = ByteString.CopyFrom(SmartContractZeroCode),
                ContractHash = Hash.FromRawBytes(SmartContractZeroCode)
            };
            var chain = await _service.CreateNewChainAsync(Hash.LoadByteArray(new byte[] { 0x01, 0x02, 0x03 }), new List<SmartContractRegistration>{reg});
            Assert.Equal(Hash.LoadByteArray(new byte[] {0x00, 0x01, 0x02, 0x03 }).DumpBase58(), chain.Id.DumpBase58());
        }
    }
}