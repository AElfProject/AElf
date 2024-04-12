using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Domain;
using AElf.Types;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.SmartContract.Application;

public interface ISmartContractBridgeService
{
    void LogDebug(Func<string> func);

    Task DeployContractAsync(ContractDto contractDto);

    Task UpdateContractAsync(ContractDto contractDto);
    
    Task<ContractInfoDto> DeployContractAsync(SmartContractRegistration registration);

    Task<ContractInfoDto> UpdateContractAsync(string previousContractVersion, SmartContractRegistration registration);

    Task<ContractVersionCheckDto> CheckContractVersionAsync(string previousContractVersion, SmartContractRegistration registration);

    Task<List<Transaction>> GetBlockTransactions(Hash blockHash);
    int GetChainId();

    Task<Address> GetAddressByContractNameAsync(IChainContext chainContext, string contractName);

    Task<IReadOnlyDictionary<Hash, Address>> GetSystemContractNameToAddressMappingAsync(IChainContext chainContext);

    Address GetZeroSmartContractAddress();

    Address GetZeroSmartContractAddress(int chainId);

    Task<ByteString> GetStateAsync(Address contractAddress, string key, long blockHeight, Hash blockHash);

    Task<int> GetStateSizeLimitAsync(IChainContext chainContext);
}

public class SmartContractBridgeService : ISmartContractBridgeService
{
    private readonly IBlockchainService _blockchainService;
    private readonly IBlockchainStateManager _blockchainStateManager;
    private readonly ISmartContractAddressService _smartContractAddressService;
    private readonly ISmartContractService _smartContractService;
    private readonly IStateSizeLimitProvider _stateSizeLimitProvider;

    public SmartContractBridgeService(ISmartContractService smartContractService,
        IBlockchainService blockchainService, ISmartContractAddressService smartContractAddressService,
        IBlockchainStateManager blockchainStateManager, IStateSizeLimitProvider stateSizeLimitProvider)
    {
        _smartContractService = smartContractService;
        _blockchainService = blockchainService;
        _smartContractAddressService = smartContractAddressService;
        _blockchainStateManager = blockchainStateManager;
        _stateSizeLimitProvider = stateSizeLimitProvider;
        Logger = NullLogger<SmartContractBridgeService>.Instance;
    }

    public ILogger<SmartContractBridgeService> Logger { get; set; }


    public void LogDebug(Func<string> func)
    {
#if DEBUG
        Logger.LogDebug(func());
#endif
    }

    public async Task DeployContractAsync(ContractDto contractDto)
    {
        await _smartContractService.DeployContractAsync(contractDto);
    }

    public async Task UpdateContractAsync(ContractDto contractDto)
    {
        await _smartContractService.UpdateContractAsync(contractDto);
    }
    
    public async Task<ContractInfoDto> DeployContractAsync(SmartContractRegistration registration)
    {
        return await _smartContractService.DeployContractAsync(registration);
    }

    public async Task<ContractInfoDto> UpdateContractAsync(string previousContractVersion,SmartContractRegistration registration)
    {
        return await _smartContractService.UpdateContractAsync(previousContractVersion,registration);
    }

    public async Task<ContractVersionCheckDto> CheckContractVersionAsync(string previousContractVersion,SmartContractRegistration registration)
    {
        return await _smartContractService.CheckContractVersionAsync(previousContractVersion, registration);
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

    public Task<Address> GetAddressByContractNameAsync(IChainContext chainContext, string contractName)
    {
        return _smartContractAddressService.GetAddressByContractNameAsync(chainContext, contractName);
    }

    public Task<IReadOnlyDictionary<Hash, Address>> GetSystemContractNameToAddressMappingAsync(
        IChainContext chainContext)
    {
        return _smartContractAddressService.GetSystemContractNameToAddressMappingAsync(chainContext);
    }

    public Address GetZeroSmartContractAddress()
    {
        return _smartContractAddressService.GetZeroSmartContractAddress();
    }

    public Address GetZeroSmartContractAddress(int chainId)
    {
        return _smartContractAddressService.GetZeroSmartContractAddress(chainId);
    }

    public Task<ByteString> GetStateAsync(Address contractAddress, string key, long blockHeight, Hash blockHash)
    {
        var address = contractAddress.ToBase58();
        if (!key.StartsWith(address))
            throw new InvalidOperationException("a contract cannot access other contracts data");

        return _blockchainStateManager.GetStateAsync(key, blockHeight,
            blockHash);
    }

    public async Task<int> GetStateSizeLimitAsync(IChainContext chainContext)
    {
        var stateSizeLimit = await _stateSizeLimitProvider.GetStateSizeLimitAsync(chainContext);
        return stateSizeLimit;
    }
}