using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel.SmartContract
{
    public interface ISmartContractContext
    {
        int ChainId { get; }
        Address ContractAddress { get; }
        ISmartContractService SmartContractService { get; }
        IBlockchainService ChainService { get; }
        
        ISmartContractExecutiveService SmartContractExecutiveService { get; }
        
#if DEBUG
        ILogger<ISmartContractContext> Logger { get; }
#endif
    }
}
