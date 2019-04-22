namespace AElf.Kernel.SmartContract
{
    public interface ISmartContractAddressNameProvider 
    {
        Hash ContractName { get; }
    }
}