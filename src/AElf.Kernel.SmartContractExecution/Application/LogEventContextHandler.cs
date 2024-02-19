using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Events;
using AElf.Types;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.SmartContractExecution.Application;

public abstract class LogEventContextHandler : ILocalEventHandler<LogEventContextData>, ITransientDependency
{
    private readonly ISmartContractAddressService _smartContractAddressService;
    private readonly ISmartContractExecutiveService _smartContractExecutiveService;
    private readonly ISmartContractRegistrationInStateProvider _smartContractRegistrationInStateProvider;
    private readonly ISmartContractRegistrationProvider _smartContractRegistrationProvider;

    public LogEventContextHandler(ISmartContractAddressService smartContractAddressService,
        ISmartContractRegistrationProvider smartContractRegistrationProvider,
        ISmartContractRegistrationInStateProvider smartContractRegistrationInStateProvider,
        ISmartContractExecutiveService smartContractExecutiveService)
    {
        _smartContractAddressService = smartContractAddressService;
        _smartContractRegistrationProvider = smartContractRegistrationProvider;
        _smartContractRegistrationInStateProvider = smartContractRegistrationInStateProvider;
        _smartContractExecutiveService = smartContractExecutiveService;
    }

    public async Task HandleEventAsync(LogEventContextData logEventContext)
    {
        // Check if the log event is either ContractDeployed or CodeUpdated
        if (!IsInterestedAsync(logEventContext.LogEvent))
        {
            return;
        }
        var chainContext = new ChainContext
        {
            BlockHash = logEventContext.Block.GetHash(),
            BlockHeight = logEventContext.Block.Height
        };
        await ProcessLogEventAsync(chainContext, logEventContext.LogEvent);
       
    }

    protected async Task SetSmartContractRegistrationAsync(ChainContext chainContext, Address address)
    {
        var smartContractRegistration =
            await _smartContractRegistrationInStateProvider.GetSmartContractRegistrationAsync(chainContext
                , address);

        await _smartContractRegistrationProvider.SetSmartContractRegistrationAsync(chainContext, address,
            smartContractRegistration);
        if (chainContext.BlockHeight > AElfConstants.GenesisBlockHeight)
            _smartContractExecutiveService.CleanExecutive(address);
    }

    protected abstract bool IsInterestedAsync(LogEvent logEvent);

    protected abstract Task ProcessLogEventAsync(ChainContext chainContext, LogEvent logEvent);


}