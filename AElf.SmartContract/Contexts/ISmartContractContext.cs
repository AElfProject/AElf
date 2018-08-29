using AElf.Kernel;

// ReSharper disable once CheckNamespace
namespace AElf.SmartContract
{
    public interface ISmartContractContext
    {
        Hash ChainId { get; }
        Hash ContractAddress { get; }
        IDataProvider DataProvider { get; }
        ISmartContractService SmartContractService { get; }
    }
}
