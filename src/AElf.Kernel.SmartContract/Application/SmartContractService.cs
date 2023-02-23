using System;
using System.Reflection;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Org.BouncyCastle.Security;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application;

public class SmartContractService : ISmartContractService, ITransientDependency
{
    private readonly ISmartContractAddressService _smartContractAddressService;
    private readonly ISmartContractRunnerContainer _smartContractRunnerContainer;
    private readonly ISmartContractExecutiveService _smartContractExecutiveService;

    public SmartContractService(ISmartContractAddressService smartContractAddressService,
        ISmartContractRunnerContainer smartContractRunnerContainer,
        ISmartContractExecutiveService smartContractExecutiveService)
    {
        _smartContractAddressService = smartContractAddressService;
        _smartContractRunnerContainer = smartContractRunnerContainer;
        _smartContractExecutiveService = smartContractExecutiveService;
    }

    public Task DeployContractAsync(ContractDto contractDto)
    {
        CheckRunner(contractDto.SmartContractRegistration.Category);
        return Task.CompletedTask;
    }

    public Task UpdateContractAsync(ContractDto contractDto)
    {
        return Task.CompletedTask;
    }
    
    /// <inheritdoc />
    public Task<ContractInfoDto> DeployContractAsync(int category,byte[] code)
    {
        CheckRunner(category);
        var contractVersion = GetVersion(code);
        return Task.FromResult(new ContractInfoDto
        {
            ContractVersion = contractVersion
        });
    }

    public async Task<ContractInfoDto> UpdateContractAsync(Address address,byte[] code,long blockHeight,Hash blockHash)
    {
        var contractVersion = GetVersion(code);
        var oldVersion = await GetVersion(address,blockHeight,blockHash);
        CheckVersion(oldVersion,contractVersion);
        return new ContractInfoDto
        {
            ContractVersion = contractVersion
        };
    }

    public async Task CheckContractVersion(Address address,byte[] code,long blockHeight,Hash blockHash)
    {
        var version = GetVersion(code);
        var oldVersion = await GetVersion(address,blockHeight,blockHash);
        CheckVersion(oldVersion,version);
    }

    private void CheckRunner(int category)
    {
        _smartContractRunnerContainer.GetRunner(category);
    }

    private string GetVersion(byte[] code)
    {
        var assembly = Assembly.Load(code);
        var version = assembly.GetName().Version?.ToString();
        return version;
    }

    private async Task<string> GetVersion(Address contractAddress, long blockHeight, Hash blockHash)
    {
        var executive = await _smartContractExecutiveService.GetExecutiveAsync(new ChainContext
        {
            BlockHeight = blockHeight,
            BlockHash = blockHash
        }, contractAddress);
        var oldVersion = executive.ContractVersion;
        return oldVersion;
    }
    
    private void CheckVersion(string oldVersion,string version)
    {
        if (!version.IsNullOrEmpty() && new Version(oldVersion) >= new Version(version))
        {
            throw new InvalidParameterException(
                $"The version to be deployed is lower than the effective version({oldVersion}), please correct the version number.");
        }
    }
}