using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Kernel.SmartContract.Sdk;
using Google.Protobuf;
using Moq;
using Xunit;

namespace AElf.Kernel.SmartContract.Application
{
    public class SmartContractExecutiveServiceTests : SmartContractRunnerTestBase
    {
        public SmartContractExecutiveServiceTests()
        {
            _smartContractExecutiveService = GetRequiredService<SmartContractExecutiveService>();
        }

        private readonly SmartContractExecutiveService _smartContractExecutiveService;

        [Fact]
        public async Task Put_ZeroRegistration_Executive_Success()
        {
            var registration = new SmartContractRegistration
            {
                Category = KernelConstants.DefaultRunnerCategory,
                Code = Hash.FromString("TestPutExecutive").ToByteString(),
                CodeHash = Hash.FromString("TestPutExecutive")
            };

            var mockExecutive = new Mock<IExecutive>();
            mockExecutive.Setup(e => e.SetTransactionContext(It.IsAny<TransactionContext>()))
                .Returns(mockExecutive.Object);
            mockExecutive.Setup(e => e.SetDataCache(It.IsAny<IStateCache>()));

            await _smartContractExecutiveService.PutExecutiveAsync(Address.Genesis, mockExecutive.Object);
        }
    }
}