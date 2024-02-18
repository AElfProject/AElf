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

public class LogEventContextHandler : ILocalEventHandler<LogEventContextData>, ITransientDependency
{
    private readonly ISmartContractAddressService _smartContractAddressService;
    private readonly ISmartContractExecutiveService _smartContractExecutiveService;
    private readonly ISmartContractRegistrationInStateProvider _smartContractRegistrationInStateProvider;
    private readonly ISmartContractRegistrationProvider _smartContractRegistrationProvider;
    public ILogger<LogEventContextHandler> _logger;

    public LogEventContextHandler(ISmartContractAddressService smartContractAddressService,
        ISmartContractRegistrationProvider smartContractRegistrationProvider,
        ISmartContractRegistrationInStateProvider smartContractRegistrationInStateProvider,
        ISmartContractExecutiveService smartContractExecutiveService,
        ILogger<LogEventContextHandler> logger)
    {
        _smartContractAddressService = smartContractAddressService;
        _smartContractRegistrationProvider = smartContractRegistrationProvider;
        _smartContractRegistrationInStateProvider = smartContractRegistrationInStateProvider;
        _smartContractExecutiveService = smartContractExecutiveService;
        _logger = logger;
    }

    public async Task HandleEventAsync(LogEventContextData logEventContext)
    {
        // Check if the log event is either ContractDeployed or CodeUpdated
        if (logEventContext.LogEvent.Name != nameof(ContractDeployed) &&
            logEventContext.LogEvent.Name != nameof(CodeUpdated))
        {
            return;
        }

        var chainContext = new ChainContext
        {
            BlockHash = logEventContext.Block.GetHash(),
            BlockHeight = logEventContext.Block.Height
        };
        // Handle the log event based on its name
        switch (logEventContext.LogEvent.Name)
        {
            case nameof(ContractDeployed):
                var contractDeployed = new ContractDeployed();
                contractDeployed.MergeFrom(logEventContext.LogEvent);
                await SetSmartContractRegistrationAsync(logEventContext, chainContext, contractDeployed.Address);
                if (contractDeployed.Name != null)
                {
                    await _smartContractAddressService.SetSmartContractAddressAsync(chainContext,
                        contractDeployed.Name.ToStorageKey(), contractDeployed.Address);
                }

                _logger.LogDebug($"Deployed contract {contractDeployed}");
                break;

            case nameof(CodeUpdated):
                var codeUpdated = new CodeUpdated();
                codeUpdated.MergeFrom(logEventContext.LogEvent);
                await SetSmartContractRegistrationAsync(logEventContext, chainContext, codeUpdated.Address);
                _logger.LogDebug($"Updated contract {codeUpdated}");
                break;
        }
    }

    private async Task SetSmartContractRegistrationAsync(LogEventContextData logEventContext, ChainContext chainContext,
        Address address)
    {
        var smartContractRegistration =
            await _smartContractRegistrationInStateProvider.GetSmartContractRegistrationAsync(chainContext
                , address);

        await _smartContractRegistrationProvider.SetSmartContractRegistrationAsync(chainContext, address,
            smartContractRegistration);
        if (logEventContext.Block.Height > AElfConstants.GenesisBlockHeight)
            _smartContractExecutiveService.CleanExecutive(address);
    }
}