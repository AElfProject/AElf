using AElf.Types;

namespace AElf.Kernel.SmartContract
{
    public interface ISmartContractAddressNameProvider 
    {
        Hash ContractName { get; }
        
        string ContractStringName { get; }
    }
}