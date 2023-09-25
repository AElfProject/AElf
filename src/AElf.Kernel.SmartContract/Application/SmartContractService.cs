using System;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application;

public class SmartContractService : ISmartContractService, ITransientDependency
{
    private readonly ISmartContractRunnerContainer _smartContractRunnerContainer;
    private readonly IHostSmartContractBridgeContextService _hostSmartContractBridgeContextService;

    public SmartContractService(
        ISmartContractRunnerContainer smartContractRunnerContainer,
        IHostSmartContractBridgeContextService hostSmartContractBridgeContextService)
    {
        _smartContractRunnerContainer = smartContractRunnerContainer;
        _hostSmartContractBridgeContextService = hostSmartContractBridgeContextService;
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

    public async Task<ContractInfoDto> UpdateContractAsync(string previousContractVersion,
        SmartContractRegistration registration)
    {
        var newContractVersion = await GetVersion(registration);
        var isSubsequentVersion = CheckVersion(previousContractVersion, newContractVersion);
        return new ContractInfoDto
        {
            ContractVersion = newContractVersion,
            IsSubsequentVersion = isSubsequentVersion
        };
    }

    public async Task<ContractVersionCheckDto> CheckContractVersionAsync(string previousContractVersion,
        SmartContractRegistration registration)
    {
        var newContractVersion = await GetVersion(registration);
        var isSubsequentVersion = CheckVersion(previousContractVersion, newContractVersion);
        return new ContractVersionCheckDto
        {
            IsSubsequentVersion = isSubsequentVersion
        };
    }

    public async Task ExecuteConstructorAsync(SmartContractRegistration registration, Address author,
        Address contractAddress, ByteString constructorInput)
    {
        if (registration.Category == KernelConstants.SolidityRunnerCategory)
        {
            //await ExecuteSolidityContractConstructorAsync(registration, author, contractAddress, constructorInput);
        }
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

    private bool CheckVersion(string previousContractVersion, string newContractVersion)
    {
        if (newContractVersion.IsNullOrEmpty())
        {
            return false;
        }

        if (previousContractVersion.IsNullOrEmpty())
        {
            return true;
        }

        return new Version(previousContractVersion) < new Version(newContractVersion);
    }

    private async Task ExecuteSolidityContractConstructorAsync(SmartContractRegistration registration, Address author,
        Address contractAddress, ByteString constructorInput)
    {
        var wasmRunner = _smartContractRunnerContainer.GetRunner(KernelConstants.SolidityRunnerCategory);
        var wasmExecutive = await wasmRunner.RunAsync(registration);
        var context = _hostSmartContractBridgeContextService.Create();
        wasmExecutive.SetHostSmartContractBridgeContext(context);
        await wasmExecutive.ApplyAsync(new TransactionContext
        {
            Origin = contractAddress,
            Transaction = new Transaction
            {
                From = author,
                To = contractAddress,
                MethodName = "deploy",
                Params = constructorInput
            },
            Trace = new TransactionTrace()
        });
    }
}