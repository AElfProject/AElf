using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.SmartContract.Domain;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.Application
{
    public class SmartContractServiceTests : SmartContractRunnerTestBase
    {
        private readonly SmartContractService _smartContractService;

        public SmartContractServiceTests()
        {
            _smartContractService = GetRequiredService<SmartContractService>();
        }

        [Fact]
        public async Task Deploy_Contract_Success()
        {
            var registration = new SmartContractRegistration
            {
                Category = 2,
                Code = Hash.FromString("TestDeployContract").ToByteString(),
                CodeHash = Hash.FromString("TestDeployContract")
            };


            await _smartContractService.DeployContractAsync(Address.Genesis, registration, false, null);

        }

        [Fact]
        public async Task Update_Contract_Success()
        {
            var registrationA = new SmartContractRegistration
            {
                Category = 2,
                Code = Hash.FromString("TestContractA").ToByteString(),
                CodeHash = Hash.FromString("TestContractA")
            };

            var registrationANew = new SmartContractRegistration
            {
                Category = 2,
                Code = Hash.FromString("TestContractA_New").ToByteString(),
                CodeHash = Hash.FromString("TestContractA")
            };

            var registrationB = new SmartContractRegistration
            {
                Category = 2,
                Code = Hash.FromString("TestContractB").ToByteString(),
                CodeHash = Hash.FromString("TestContractB")
            };

            await _smartContractService.DeployContractAsync(Address.Genesis, registrationA, false, null);
            await _smartContractService.UpdateContractAsync(Address.Genesis, registrationANew, false, null);


            await _smartContractService.UpdateContractAsync(Address.Genesis, registrationB, false, null);

        }
    }
}