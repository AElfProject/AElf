using AElf.Common;

namespace AElf.Kernel.SmartContract.Sdk
{
    public class SmartContractContext : ISmartContractContext
    {
        public Address ContractAddress { get; set; }
        public int RunnerCategory { get; set; }
    }
}