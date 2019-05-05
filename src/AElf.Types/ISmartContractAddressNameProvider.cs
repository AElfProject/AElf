using AElf.Types;

namespace AElf
{
    public interface ISmartContractAddressNameProvider 
    {
        Hash ContractName { get; }
    }
}