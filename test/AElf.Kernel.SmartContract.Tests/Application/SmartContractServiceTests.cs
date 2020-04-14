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
                Code = Hash.ComputeFrom("TestDeployContract").ToByteString(),
                CodeHash = Hash.ComputeFrom("TestDeployContract")
            };


            await _smartContractService.DeployContractAsync(new ContractDto
            {
                BlockHeight = 1, 
                ContractAddress = SampleAddress.AddressList[0], 
                ContractName = null,
                IsPrivileged = false, 
                SmartContractRegistration = registration
            });

        }

        [Fact]
        public async Task Update_Contract_Success()
        {
            var registrationA = new SmartContractRegistration
            {
                Category = KernelConstants.DefaultRunnerCategory,
                Code = Hash.ComputeFrom("TestContractA").ToByteString(),
                CodeHash = Hash.ComputeFrom("TestContractA")
            };

            var registrationANew = new SmartContractRegistration
            {
                Category = KernelConstants.DefaultRunnerCategory,
                Code = Hash.ComputeFrom("TestContractA_New").ToByteString(),
                CodeHash = Hash.ComputeFrom("TestContractA")
            };

            var registrationB = new SmartContractRegistration
            {
                Category = KernelConstants.DefaultRunnerCategory,
                Code = Hash.ComputeFrom("TestContractB").ToByteString(),
                CodeHash = Hash.ComputeFrom("TestContractB")
            };

            await _smartContractService.DeployContractAsync(new ContractDto
            {
                BlockHeight = 1, 
                ContractAddress = SampleAddress.AddressList[0], 
                ContractName = null,
                IsPrivileged = false, 
                SmartContractRegistration = registrationA
            });
            
            await _smartContractService.UpdateContractAsync(new ContractDto
            {
                ContractAddress = SampleAddress.AddressList[1],
                SmartContractRegistration = registrationANew,
                BlockHeight = 2,
                IsPrivileged = false,
                ContractName = null
            });
            
            await _smartContractService.UpdateContractAsync(new ContractDto
            {
                ContractAddress = SampleAddress.AddressList[2],
                SmartContractRegistration = registrationB,
                BlockHeight = 2,
                IsPrivileged = false,
                ContractName = null
            });
        }
    }
}