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
    private readonly ISmartContractRunner _smartContractRunner;

    public SmartContractService(
        ISmartContractRunnerContainer smartContractRunnerContainer,
        ISmartContractRunner smartContractRunner)
    {
        _smartContractRunnerContainer = smartContractRunnerContainer;
        _smartContractRunner = smartContractRunner;
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
        CheckRunner(registration.Category);
        var contractVersion = await GetVersion(registration);
        return new ContractInfoDto
        {
            ContractVersion = contractVersion
        };
    }

    public async Task<ContractInfoDto> UpdateContractAsync(string contractVersion,SmartContractRegistration registration)
    {
        var newContractVersion = await GetVersion(registration);
        var isContractVersionCorrect = CheckVersion(contractVersion,newContractVersion);
        return new ContractInfoDto
        {
            ContractVersion = contractVersion,
            IsContractVersionCorrect = isContractVersionCorrect
        };
    }

    public async Task<ContractVersionCheckDto> CheckContractVersion(string contractVersion,SmartContractRegistration registration)
    {
        var newContractVersion = await GetVersion(registration);
        var isContractVersionCorrect = CheckVersion(contractVersion,newContractVersion);
        return new ContractVersionCheckDto
        {
            IsContractVersionCorrect = isContractVersionCorrect
        };
    }

    private void CheckRunner(int category)
    {
        _smartContractRunnerContainer.GetRunner(category);
    }

    private async Task<string> GetVersion(SmartContractRegistration registration)
    {
        var executive = await _smartContractRunner.RunAsync(registration);
        var contractVersion = executive.ContractVersion;
        return contractVersion;
    }
    
    private bool CheckVersion(string oldVersion,string version)
    {
        return version.IsNullOrEmpty() || new Version(oldVersion) < new Version(version);
    }
}