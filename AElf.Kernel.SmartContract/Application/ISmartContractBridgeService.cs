using System;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel.Blockchain.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.SmartContract.Application
{
    public interface ISmartContractBridgeService
    {
        void LogDebug(Func<string> func);

        Task DeployContractAsync(Address contractAddress, SmartContractRegistration registration,
            bool isPrivileged, Hash name);

        Task UpdateContractAsync(Address contractAddress, SmartContractRegistration registration,
            bool isPrivileged, Hash name);

        Task<Block> GetBlockByHashAsync(Hash blockId);
        int GetChainId();

        Address GetAddressByContractName(Hash contractName);

        Address GetZeroSmartContractAddress();
    }

    public class SmartContractBridgeService : ISmartContractBridgeService
    {
        private readonly ISmartContractService _smartContractService;
        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractAddressService _smartContractAddressService;

        public ILogger<SmartContractBridgeService> Logger { get; set; }

        public SmartContractBridgeService(ISmartContractService smartContractService,
            IBlockchainService blockchainService, ISmartContractAddressService smartContractAddressService)
        {
            _smartContractService = smartContractService;
            _blockchainService = blockchainService;
            _smartContractAddressService = smartContractAddressService;
            Logger = NullLogger<SmartContractBridgeService>.Instance;
        }

        public void LogDebug(Func<string> func)
        {
            Logger.LogDebug(func());
        }

        public async Task DeployContractAsync(Address contractAddress, SmartContractRegistration registration,
            bool isPrivileged, Hash name)
        {
            await _smartContractService.DeployContractAsync(contractAddress, registration, isPrivileged, name);
        }

        public async Task UpdateContractAsync(Address contractAddress, SmartContractRegistration registration,
            bool isPrivileged, Hash name)
        {
            await _smartContractService.UpdateContractAsync(contractAddress, registration, isPrivileged, name);
        }

        public async Task<Block> GetBlockByHashAsync(Hash blockId)
        {
            return await _blockchainService.GetBlockByHashAsync(blockId);
        }

        public int GetChainId()
        {
            return _blockchainService.GetChainId();
        }

        public Address GetAddressByContractName(Hash contractName)
        {
            return _smartContractAddressService.GetAddressByContractName(contractName);
        }

        public Address GetZeroSmartContractAddress()
        {
            return _smartContractAddressService.GetZeroSmartContractAddress();
        }
    }
}