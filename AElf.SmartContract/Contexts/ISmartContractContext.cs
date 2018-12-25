using AElf.Common;
using AElf.Kernel;

// ReSharper disable once CheckNamespace
namespace AElf.SmartContract
{
    public interface ISmartContractContext
    {
        Hash ChainId { get; }
        Address ContractAddress { get; }
        IDataProvider DataProvider { get; }
        ISmartContractService SmartContractService { get; }
        IChainService ChainService { get; }
    }
}
