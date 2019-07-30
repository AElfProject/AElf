using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf;
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
                Category = KernelConstants.DefaultRunnerCategory,
                Code = Hash.FromString("TestDeployContract").ToByteString(),
                CodeHash = Hash.FromString("TestDeployContract")
            };


            await _smartContractService.DeployContractAsync(SampleAddress.AddressList[0], registration, false, null);

        }

        [Fact]
        public async Task Update_Contract_Success()
        {
            var registrationA = new SmartContractRegistration
            {
                Category = KernelConstants.DefaultRunnerCategory,
                Code = Hash.FromString("TestContractA").ToByteString(),
                CodeHash = Hash.FromString("TestContractA")
            };

            var registrationANew = new SmartContractRegistration
            {
                Category = KernelConstants.DefaultRunnerCategory,
                Code = Hash.FromString("TestContractA_New").ToByteString(),
                CodeHash = Hash.FromString("TestContractA")
            };

            var registrationB = new SmartContractRegistration
            {
                Category = KernelConstants.DefaultRunnerCategory,
                Code = Hash.FromString("TestContractB").ToByteString(),
                CodeHash = Hash.FromString("TestContractB")
            };

            await _smartContractService.DeployContractAsync(SampleAddress.AddressList[0], registrationA, false, null);
            
            await _smartContractService.UpdateContractAsync(new UpdateContractDto
            {
                ContractAddress = SampleAddress.AddressList[1],
                SmartContractRegistration = registrationANew,
                BlockHeight = 1,
                PreviousBlockHash = Hash.Empty,
                IsPrivileged = false,
                ContractName = null
            });
            
            await _smartContractService.UpdateContractAsync(new UpdateContractDto
            {
                ContractAddress = SampleAddress.AddressList[2],
                SmartContractRegistration = registrationB,
                BlockHeight = 1,
                PreviousBlockHash = Hash.Empty,
                IsPrivileged = false,
                ContractName = null
            });

        }
    }
}