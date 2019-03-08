using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;

// ReSharper disable once CheckNamespace
namespace AElf.Kernel.SmartContract
{
    public interface ISmartContractContext
    {
        Address ContractAddress { get; }

        void LogDebug(Func<string> func);

        Task DeployContractAsync(Address contractAddress, SmartContractRegistration registration,
            bool isPrivileged, Hash name);

        Task UpdateContractAsync(Address contractAddress, SmartContractRegistration registration,
            bool isPrivileged, Hash name);

        Task<Block> GetBlockByHashAsync(Hash blockId);
        Block GetBlockByHash(Hash blockId);

        void DeployContract(Address contractAddress, SmartContractRegistration registration,
            bool isPrivileged, Hash name);

        void UpdateContract(Address contractAddress, SmartContractRegistration registration,
            bool isPrivileged, Hash name);

        int GetChainId();

        Address GetAddressByContractName(Hash contractName);

        Address GetZeroSmartContractAddress();
    }
}