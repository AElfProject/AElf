using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using Microsoft.Extensions.Logging;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel.SmartContract
{
    public interface ISmartContractContext
    {
        int ChainId { get; }
        Address ContractAddress { get; }

#if DEBUG
        ILogger<ISmartContractContext> Logger { get; }
#endif
        Task DeployContractAsync(int chainId, Address contractAddress, SmartContractRegistration registration,
            bool isPrivileged);

        Task UpdateContractAsync(int chainId, Address contractAddress, SmartContractRegistration registration,
            bool isPrivileged);
    }
}