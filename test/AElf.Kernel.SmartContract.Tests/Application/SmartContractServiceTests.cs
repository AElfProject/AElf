using System;
using System.Threading.Tasks;
using AElf.Types;
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
                Category = KernelConstants.DefaultRunnerCategory,
                Code = HashHelper.ComputeFrom("TestDeployContract").ToByteString(),
                CodeHash = HashHelper.ComputeFrom("TestDeployContract")
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
                Code = HashHelper.ComputeFrom("TestContractA").ToByteString(),
                CodeHash = HashHelper.ComputeFrom("TestContractA")
            };

            var registrationANew = new SmartContractRegistration
            {
                Category = KernelConstants.DefaultRunnerCategory,
                Code = HashHelper.ComputeFrom("TestContractA_New").ToByteString(),
                CodeHash = HashHelper.ComputeFrom("TestContractA")
            };

            var registrationB = new SmartContractRegistration
            {
                Category = KernelConstants.DefaultRunnerCategory,
                Code = HashHelper.ComputeFrom("TestContractB").ToByteString(),
                CodeHash = HashHelper.ComputeFrom("TestContractB")
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

        [Fact]
        public void ExceptionTest()
        {
            var message = "message";
            var exception = new Exception();
            Should.Throw<SmartContractExecutingException>(() => throw new SmartContractExecutingException());
            Should.Throw<SmartContractExecutingException>(() => throw new SmartContractExecutingException(message, exception));
            Should.Throw<SmartContractFindRegistrationException>(() => throw new SmartContractFindRegistrationException());
            Should.Throw<SmartContractFindRegistrationException>(() => throw new SmartContractFindRegistrationException(message, exception));
        }
    }
}