using System;
using System.Reflection;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Infrastructure;
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

    /// <inheritdoc />
    public Task<ContractInfoDto> DeployContractAsync(ContractDto contractDto)
    {
        CheckRunner(contractDto.SmartContractRegistration.Category);
        var assembly = Assembly.Load(contractDto.SmartContractRegistration.Code.ToByteArray());
        var contractVersion = assembly.GetName().Version?.ToString();
        return Task.FromResult(new ContractInfoDto
        {
            ContractVersion = contractVersion
        });
    }

    public async Task<ContractInfoDto> UpdateContractAsync(ContractDto contractDto)
    {
        var contractVersion = await CheckContractVersion(contractDto);
        return new ContractInfoDto
        {
            ContractVersion = contractVersion
        };
    }

    private void CheckRunner(int category)
    {
        _smartContractRunnerContainer.GetRunner(category);
    }

    private async Task<string> CheckContractVersion(ContractDto contractDto)
    {
        //get new version.
        var assembly = Assembly.Load(contractDto.SmartContractRegistration.Code.ToByteArray());
        var version = assembly.GetName().Version?.ToString();
        //get old version from executive.
        var executive = await _smartContractExecutiveService.GetExecutiveAsync(new ChainContext
        {
            BlockHeight = contractDto.BlockHeight,
            BlockHash = contractDto.PreviousBlockHash
        }, contractDto.ContractAddress);
        var oldVersion = executive.ContractVersion;
        //check version.
        if (version != null && new Version(oldVersion) >= new Version(version))
        {
            throw new InvalidParameterException(
                $"The version to be deployed is lower than the effective version({oldVersion}), please correct the version number.");
        }
        return version;
    }
}