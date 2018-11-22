using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ChainController;
using AElf.Common.Extensions;
using Google.Protobuf;
using Xunit;
using Xunit.Frameworks.Autofac;
using AElf.Common;

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
                ContractHash = Hash.FromRawBytes(SmartContractZeroCode)
            };
            var chain = await _service.CreateNewChainAsync(Hash.LoadByteArray(new byte[] { 0x01, 0x02, 0x03 }), new List<SmartContractRegistration>{reg});
            Assert.Equal(Hash.LoadByteArray(new byte[] { 0x01, 0x02, 0x03 }).DumpBase58(), chain.Id.DumpBase58());
        }
    }
}