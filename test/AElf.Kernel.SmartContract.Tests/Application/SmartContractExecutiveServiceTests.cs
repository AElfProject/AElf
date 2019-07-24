using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Google.Protobuf;
using Moq;
using Xunit;

namespace AElf.Kernel.SmartContract.Application
{
    public class SmartContractExecutiveServiceTests : SmartContractRunnerTestBase
    {
        private readonly SmartContractExecutiveService _smartContractExecutiveService;

        public SmartContractExecutiveServiceTests()
        {
            _smartContractExecutiveService = GetRequiredService<SmartContractExecutiveService>();
        }

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
            await _smartContractExecutiveService.PutExecutiveAsync(SampleAddress.AddressList[7], mockExecutive.Object);
        }

    }
}