using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel.SmartContract
{
    public class SmartContractContext : ISmartContractContext
    {
        public int ChainId { get; set; }
        public Address ContractAddress { get; set; }
        public ISmartContractService SmartContractService { get; set; }
        public IBlockchainService ChainService { get; set; }
        public ISmartContractExecutiveService SmartContractExecutiveService { get; set; }

#if DEBUG
        public ILogger<ISmartContractContext> Logger { get; set; } = NullLogger<ISmartContractContext>.Instance;

        public Task DeployContractAsync(int chainId, Address contractAddress, SmartContractRegistration registration,
            bool isPrivileged)
        {
            return SmartContractService.DeployContractAsync(chainId, contractAddress, registration, isPrivileged);
        }

        public Task UpdateContractAsync(int chainId, Address contractAddress, SmartContractRegistration registration,
            bool isPrivileged)
        {
            return SmartContractService.UpdateContractAsync(chainId, contractAddress, registration, isPrivileged);
        }
#endif
    }
}