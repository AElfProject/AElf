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
    private readonly ISmartContractRunnerContainer _smartContractRunnerContainer;

    public SmartContractService(
        ISmartContractRunnerContainer smartContractRunnerContainer)
    {
        _smartContractRunnerContainer = smartContractRunnerContainer;
    }

    /// <inheritdoc />
    public Task DeployContractAsync(ContractDto contractDto)
    {
        CheckRunner(contractDto.SmartContractRegistration.Category);
        return Task.CompletedTask;
    }

    public Task UpdateContractAsync(ContractDto contractDto)
    {
        return Task.CompletedTask;
    }
    
    public async Task<ContractInfoDto> DeployContractAsync(SmartContractRegistration registration)
    {
        var contractVersion = await GetVersion(registration);
        return new ContractInfoDto
        {
            ContractVersion = contractVersion
        };
    }

    public async Task<ContractInfoDto> UpdateContractAsync(string previousContractVersion,SmartContractRegistration registration)
    {
        var newContractVersion = await GetVersion(registration);
        var isSubsequentVersion = CheckVersion(previousContractVersion,newContractVersion);
        return new ContractInfoDto
        {
            ContractVersion = newContractVersion,
            IsSubsequentVersion = isSubsequentVersion
        };
    }

    public async Task<ContractVersionCheckDto> CheckContractVersionAsync(string previousContractVersion,SmartContractRegistration registration)
    {
        var newContractVersion = await GetVersion(registration);
        var isSubsequentVersion = CheckVersion(previousContractVersion,newContractVersion);
        return new ContractVersionCheckDto
        {
            IsSubsequentVersion = isSubsequentVersion
        };
    }

    private void CheckRunner(int category)
    {
        _smartContractRunnerContainer.GetRunner(category);
    }

    private async Task<string> GetVersion(SmartContractRegistration registration)
    {
        var runner = _smartContractRunnerContainer.GetRunner(registration.Category);
        var executive = await runner.RunAsync(registration);
        var contractVersion = executive.ContractVersion;
        return contractVersion;
    }
    
    
    private bool CheckVersion(string previousContractVersion,string newContractVersion)
    {
        if (newContractVersion.IsNullOrEmpty())
        {
            return false;
        }

        if (previousContractVersion.IsNullOrEmpty())
        {
            return true;
        }

        return  new Version(previousContractVersion) < new Version(newContractVersion);
    }
}