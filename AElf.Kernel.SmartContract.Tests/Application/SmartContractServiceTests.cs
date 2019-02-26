using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.SmartContract.Domain;
using Google.Protobuf;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.Application
{
    public class SmartContractServiceTests : SmartContractTestBase
    {
        private readonly SmartContractService _smartContractService;
        private readonly ISmartContractManager _smartContractManager;
        private int _chainId = 1;
        
        public SmartContractServiceTests()
        {
            _smartContractService = GetRequiredService<SmartContractService>();
            _smartContractManager = GetRequiredService<ISmartContractManager>();
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

            var existRegistration = await _smartContractManager.GetAsync(registration.CodeHash);
            existRegistration.ShouldBeNull();

            await _smartContractService.DeployContractAsync(_chainId, Address.Genesis, registration, false);
            
            existRegistration = await _smartContractManager.GetAsync(registration.CodeHash);
            existRegistration.ShouldNotBeNull();
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
            
            await _smartContractService.DeployContractAsync(_chainId, Address.Genesis, registrationA, false);
            await _smartContractService.UpdateContractAsync(_chainId, Address.Genesis, registrationANew, false);
            
            var existRegistrationA = await _smartContractManager.GetAsync(registrationA.CodeHash);
            existRegistrationA.Code.ShouldBe(registrationANew.Code);
            
            await _smartContractService.UpdateContractAsync(_chainId, Address.Genesis, registrationB, false);
            
            existRegistrationA = await _smartContractManager.GetAsync(registrationA.CodeHash);
            existRegistrationA.Code.ShouldBe(registrationANew.Code);
        }
    }
}