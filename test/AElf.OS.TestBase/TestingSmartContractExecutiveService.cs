using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContract.Infrastructure;

namespace AElf.OS
{
    public class TestingSmartContractExecutiveService : SmartContractExecutiveService
    {
        public TestingSmartContractExecutiveService(ISmartContractExecutiveProvider smartContractExecutiveProvider) : base(
            smartContractExecutiveProvider)
        {
        }
    }
}