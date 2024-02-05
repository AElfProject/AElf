using System.Threading.Tasks;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Events;
using AElf.Standards.ACS0;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.SmartContractExecution.Application;

public class LogEventDataEventHandler : ILocalEventHandler<LogEventDataEvent>, ITransientDependency
{
    private readonly ISmartContractAddressService _smartContractAddressService;
    private readonly ISmartContractExecutiveService _smartContractExecutiveService;
    private readonly ISmartContractRegistrationInStateProvider _smartContractRegistrationInStateProvider;
    private readonly ISmartContractRegistrationProvider _smartContractRegistrationProvider;
    public ILogger<LogEventDataEventHandler> _logger;

    public LogEventDataEventHandler(ISmartContractAddressService smartContractAddressService,
        ISmartContractRegistrationProvider smartContractRegistrationProvider,
        ISmartContractRegistrationInStateProvider smartContractRegistrationInStateProvider,
        ISmartContractExecutiveService smartContractExecutiveService,
        ILogger<LogEventDataEventHandler> logger)
    {
        _smartContractAddressService = smartContractAddressService;
        _smartContractRegistrationProvider = smartContractRegistrationProvider;
        _smartContractRegistrationInStateProvider = smartContractRegistrationInStateProvider;
        _smartContractExecutiveService = smartContractExecutiveService;
        _logger = logger;
    }

    public async Task HandleEventAsync(LogEventDataEvent logEvent)
    {
        _logger.LogDebug("LogEventDataEvent Handler BlockHeight {BlockHeight} , LogEventName {LogEventName}",
            logEvent.Block.Height, logEvent.LogEvent.Name);

        // Check if the log event is either ContractDeployed or CodeUpdated
        if (logEvent.LogEvent.Name != nameof(ContractDeployed) && logEvent.LogEvent.Name != nameof(CodeUpdated))
        {
            return;
        }

        var chainContext = new ChainContext
        {
            BlockHash = logEvent.Block.GetHash(),
            BlockHeight = logEvent.Block.Height
        };
        // Handle the log event based on its name
        switch (logEvent.LogEvent.Name)
        {
            case nameof(ContractDeployed):
                var contractDeployed = new ContractDeployed();
                contractDeployed.MergeFrom(logEvent.LogEvent);
                await SetSmartContractRegistrationAsync(logEvent, chainContext, contractDeployed.Address);
                if (contractDeployed.Name != null)
                {
                    await _smartContractAddressService.SetSmartContractAddressAsync(chainContext,
                        contractDeployed.Name.ToStorageKey(), contractDeployed.Address);
                }

                _logger.LogDebug($"Deployed contract {contractDeployed}");
                break;

            case nameof(CodeUpdated):
                var codeUpdated = new CodeUpdated();
                codeUpdated.MergeFrom(logEvent.LogEvent);
                await SetSmartContractRegistrationAsync(logEvent, chainContext, codeUpdated.Address);
                _logger.LogDebug($"Updated contract {codeUpdated}");
                break;
        }
    }

    private async Task SetSmartContractRegistrationAsync(LogEventDataEvent logEvent, ChainContext chainContext,
        Address address)
    {
        var smartContractRegistration =
            await _smartContractRegistrationInStateProvider.GetSmartContractRegistrationAsync(chainContext
                , address);

        await _smartContractRegistrationProvider.SetSmartContractRegistrationAsync(chainContext, address,
            smartContractRegistration);
        if (logEvent.Block.Height > AElfConstants.GenesisBlockHeight)
            _smartContractExecutiveService.CleanExecutive(address);
    }
}