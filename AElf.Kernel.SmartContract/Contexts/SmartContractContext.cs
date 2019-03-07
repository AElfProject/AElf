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
        public Address ContractAddress { get; set; }
        public ISmartContractService SmartContractService { get; set; }
        public IBlockchainService BlockchainService { get; set; }
        public ISmartContractExecutiveService SmartContractExecutiveService { get; set; }

        public ISmartContractAddressService SmartContractAddressService { get; set; }

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

        public void DeployContract(Address contractAddress, SmartContractRegistration registration, bool isPrivileged,
            Hash name = null)
        {
            AsyncHelper.RunSync(() => DeployContractAsync(contractAddress, registration, isPrivileged, name));
        }

        public void UpdateContract(Address contractAddress, SmartContractRegistration registration, bool isPrivileged,
            Hash name = null)
        {
            AsyncHelper.RunSync(() => UpdateContractAsync(contractAddress, registration, isPrivileged, name));
        }

        public int GetChainId()
        {
            return BlockchainService.GetChainId();
        }

        public Address GetAddressByContractName(Hash contractName)
        {
            return SmartContractAddressService.GetAddressByContractName(contractName);
        }

        public Address GetZeroSmartContractAddress()
        {
            return SmartContractAddressService.GetZeroSmartContractAddress();
        }

        public Task DeployContractAsync(Address contractAddress, SmartContractRegistration registration,
            bool isPrivileged, Hash name)
        {
            return SmartContractService.DeployContractAsync(contractAddress, registration, isPrivileged, name);
        }

        public Task UpdateContractAsync(Address contractAddress, SmartContractRegistration registration,
            bool isPrivileged, Hash name)
        {
            return SmartContractService.UpdateContractAsync(contractAddress, registration, isPrivileged, name);
        }

        public Task<Block> GetBlockByHashAsync(Hash blockId)
        {
            return BlockchainService.GetBlockByHashAsync(blockId);
        }
    }
}