using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Domain;
using AElf.Kernel.SmartContract.Infrastructure;
using AElf.Types;
using Google.Protobuf;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.SmartContract.Application;

public class SmartContractService : ISmartContractService, ITransientDependency
{
    private readonly ISmartContractAddressService _smartContractAddressService;
    private readonly ISmartContractRunnerContainer _smartContractRunnerContainer;
    private readonly ISmartContractCodeManager _smartContractCodeManager;

    public SmartContractService(ISmartContractAddressService smartContractAddressService,
        ISmartContractRunnerContainer smartContractRunnerContainer, ISmartContractCodeManager smartContractCodeManager)
    {
        _smartContractAddressService = smartContractAddressService;
        _smartContractRunnerContainer = smartContractRunnerContainer;
        _smartContractCodeManager = smartContractCodeManager;
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

    public async Task<ByteString> GetSmartContractCodeAsync(Hash originCodeHash)
    {
        var code = await _smartContractCodeManager.GetSmartContractCodeAsync(originCodeHash);
        return code?.PatchedCode;
    }
    
    public async Task AddSmartContractCodeAsync(Hash originCodeHash, ByteString patchedCode)
    {
        await _smartContractCodeManager.AddSmartContractCodeAsync(new SmartContractCode
        {
            OriginCodeHash = originCodeHash,
            PatchedCode = patchedCode
        });
    }

    private void CheckRunner(int category)
    {
        _smartContractRunnerContainer.GetRunner(category);
    }
}