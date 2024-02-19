using System.Threading.Tasks;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Standards.ACS0;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.SmartContractExecution.Application;

public class ContractDeployedHandler : LogEventContextHandler
{
    private readonly ISmartContractAddressService _smartContractAddressService;
    public ILogger<ContractDeployedHandler> Logger { get; set; }


    public ContractDeployedHandler(ISmartContractAddressService smartContractAddressService,
        ISmartContractRegistrationProvider smartContractRegistrationProvider,
        ISmartContractRegistrationInStateProvider smartContractRegistrationInStateProvider,
        ISmartContractExecutiveService smartContractExecutiveService
    ) : base(smartContractAddressService, smartContractRegistrationProvider,
        smartContractRegistrationInStateProvider, smartContractExecutiveService)
    {
        _smartContractAddressService = smartContractAddressService;
        Logger = NullLogger<ContractDeployedHandler>.Instance;
    }


    protected override bool IsInterestedAsync(LogEvent logEvent)
    {
        var address = _smartContractAddressService.GetZeroSmartContractAddress();
        if (address == null) return false;
        if (logEvent.Address.Equals(address) && logEvent.Name.Equals(nameof(ContractDeployed)))
            return true;
        return false;
    }

    protected override async Task ProcessLogEventAsync(ChainContext chainContext, LogEvent logEvent)
    {
        var contractDeployed = new ContractDeployed();
        contractDeployed.MergeFrom(logEvent);
        await SetSmartContractRegistrationAsync(chainContext, contractDeployed.Address);
        if (contractDeployed.Name != null)
        {
            await _smartContractAddressService.SetSmartContractAddressAsync(chainContext,
                contractDeployed.Name.ToStorageKey(), contractDeployed.Address);
        }

        Logger.LogDebug($"Deployed contract {contractDeployed}");
    }
}