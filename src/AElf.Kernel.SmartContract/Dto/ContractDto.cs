using AElf.Types;

namespace AElf.Kernel.SmartContract
{
    public class ContractDto
    {
        public Address ContractAddress { get; set; }

        public SmartContractRegistration SmartContractRegistration { get; set; }
        
        public long BlockHeight { get; set; }

        public bool IsPrivileged { get; set; }

        public Hash ContractName { get; set; } = null;
    }
}