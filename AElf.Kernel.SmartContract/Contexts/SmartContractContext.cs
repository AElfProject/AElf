using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.Threading;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel.SmartContract
{
    public class SmartContractContext : ISmartContractContext
    {
        public int ChainId { get; set; }
        public Address ContractAddress { get; set; }
        public ISmartContractService SmartContractService { get; set; }
        public IBlockchainService BlockchainService { get; set; }
        public ISmartContractExecutiveService SmartContractExecutiveService { get; set; }

#if DEBUG
        public ILogger<ISmartContractContext> Logger { get; set; } = NullLogger<ISmartContractContext>.Instance;

#endif
        public void LogDebug(Func<string> func)
        {
#if DEBUG
            Logger.LogDebug(func());
#endif
        }


        public Block GetBlockByHash(Hash blockId)
        {
            return AsyncHelper.RunSync(() => GetBlockByHashAsync(blockId));
        }

        public void DeployContract(Address contractAddress, SmartContractRegistration registration, bool isPrivileged)
        {
            AsyncHelper.RunSync(() => DeployContractAsync(contractAddress, registration, isPrivileged));
        }

        public void UpdateContract(Address contractAddress, SmartContractRegistration registration, bool isPrivileged)
        {
            AsyncHelper.RunSync(() => UpdateContractAsync(contractAddress, registration, isPrivileged));
        }

        public Task DeployContractAsync(Address contractAddress, SmartContractRegistration registration,
            bool isPrivileged)
        {
            return SmartContractService.DeployContractAsync(ChainId, contractAddress, registration, isPrivileged);
        }

        public Task UpdateContractAsync(Address contractAddress, SmartContractRegistration registration,
            bool isPrivileged)
        {
            return SmartContractService.UpdateContractAsync(ChainId, contractAddress, registration, isPrivileged);
        }

        public Task<Block> GetBlockByHashAsync(Hash blockId)
        {
            return BlockchainService.GetBlockByHashAsync(blockId);
        }
    }
}