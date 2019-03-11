using AElf.Common;

namespace AElf.Kernel.SmartContract.Sdk
{
    public interface ISmartContractContext
    {
        Address ContractAddress { get; }
    }
}