using AElf.Types;

namespace AElf.Kernel.SmartContract
{
    public class UpdateContractDto
    {
        public Address ContractAddress { get; set; }

        public SmartContractRegistration SmartContractRegistration { get; set; }
        
        public long BlockHeight { get; set; }
        
        public Hash PreviousBlockHash { get; set; }

        public bool IsPrivileged { get; set; }

        public Hash ContractName { get; set; } = null;
    }
}