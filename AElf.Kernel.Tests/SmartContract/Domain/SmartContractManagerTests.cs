using System.Threading.Tasks;
using AElf.Common;
using Shouldly;
using Xunit;

namespace AElf.Kernel.SmartContract.Domain
{
    public class SmartContractManagerTests : AElfKernelTestBase
    {
        private readonly SmartContractManager _smartContractManager;
        public SmartContractManagerTests()
        {
            _smartContractManager = GetRequiredService<SmartContractManager>();
        }

        [Fact]
        public async Task Add_SmartContract_Success()
        {
            var hashKey = Hash.FromString("TestSmartContractAdd");
            
            var contract = await _smartContractManager.GetAsync(hashKey);
            contract.ShouldBeNull();

            var newContract = new SmartContractRegistration
            {
                CodeHash = hashKey
            };
            await _smartContractManager.InsertAsync(newContract);
            
            contract = await _smartContractManager.GetAsync(hashKey);
            contract.ShouldNotBeNull();
        }
    }
}