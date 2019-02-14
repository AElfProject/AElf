using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;

// ReSharper disable once CheckNamespace
namespace AElf.SmartContract
{
    public interface ISmartContractContext
    {
        int ChainId { get; }
        Address ContractAddress { get; }
        IDataProvider DataProvider { get; }
        ISmartContractService SmartContractService { get; }
        IBlockchainService ChainService { get; }
    }
}
