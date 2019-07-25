using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Types;
using Google.Protobuf;
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

        Task<List<Transaction>> GetBlockTransactions(Hash blockHash);
        int GetChainId();

        Address GetAddressByContractName(Hash contractName);

        IReadOnlyDictionary<Hash, Address> GetSystemContractNameToAddressMapping();

        Address GetZeroSmartContractAddress();

        Task<ByteString> GetStateAsync(Address contractAddress, string key, long blockHeight, Hash blockHash);
    }

    public class SmartContractBridgeService : ISmartContractBridgeService
    {
        private readonly ISmartContractService _smartContractService;
        private readonly IBlockchainService _blockchainService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IBlockchainStateManager _blockchainStateManager;

        public ILogger<SmartContractBridgeService> Logger { get; set; }

        public SmartContractBridgeService(ISmartContractService smartContractService,
            IBlockchainService blockchainService, ISmartContractAddressService smartContractAddressService,
            IBlockchainStateManager blockchainStateManager)
        {
            _smartContractService = smartContractService;
            _blockchainService = blockchainService;
            _smartContractAddressService = smartContractAddressService;
            _blockchainStateManager = blockchainStateManager;
            Logger = NullLogger<SmartContractBridgeService>.Instance;
        }


        public void LogDebug(Func<string> func)
        {
#if DEBUG
            Logger.LogDebug(func());
#endif
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

        public async Task<List<Transaction>> GetBlockTransactions(Hash blockHash)
        {
            var block = await _blockchainService.GetBlockByHashAsync(blockHash);
            return await _blockchainService.GetTransactionsAsync(block.Body.TransactionIds);
        }

        public int GetChainId()
        {
            return _blockchainService.GetChainId();
        }

        public Address GetAddressByContractName(Hash contractName)
        {
            return _smartContractAddressService.GetAddressByContractName(contractName);
        }
        
        public IReadOnlyDictionary<Hash, Address> GetSystemContractNameToAddressMapping()
        {
            return _smartContractAddressService.GetSystemContractNameToAddressMapping();
        }

        public Address GetZeroSmartContractAddress()
        {
            return _smartContractAddressService.GetZeroSmartContractAddress();
        }

        public Task<ByteString> GetStateAsync(Address contractAddress, string key, long blockHeight, Hash blockHash)
        {
            var address = contractAddress.GetFormatted();
            if(!key.StartsWith(address))
                throw new InvalidOperationException("a contract cannot access other contracts data");
            
            return _blockchainStateManager.GetStateAsync(key, blockHeight,
                blockHash);
        }
    }
}