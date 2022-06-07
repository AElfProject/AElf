using System.Threading.Tasks;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.SmartContract.Application;
using AElf.Standards.ACS0;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.SmartContractExecution.Application;

public class CodeUpdatedLogEventProcessor : LogEventProcessorBase, IBlockAcceptedLogEventProcessor
{
    private readonly ISmartContractAddressService _smartContractAddressService;
    private readonly ISmartContractExecutiveService _smartContractExecutiveService;
    private readonly ISmartContractRegistrationInStateProvider _smartContractRegistrationInStateProvider;
    private readonly ISmartContractRegistrationProvider _smartContractRegistrationProvider;

    public CodeUpdatedLogEventProcessor(ISmartContractAddressService smartContractAddressService,
        ISmartContractRegistrationProvider smartContractRegistrationProvider,
        ISmartContractRegistrationInStateProvider smartContractRegistrationInStateProvider,
        ISmartContractExecutiveService smartContractExecutiveService)
    {
        _smartContractAddressService = smartContractAddressService;
        _smartContractRegistrationProvider = smartContractRegistrationProvider;
        _smartContractRegistrationInStateProvider = smartContractRegistrationInStateProvider;
        _smartContractExecutiveService = smartContractExecutiveService;

        Logger = NullLogger<CodeUpdatedLogEventProcessor>.Instance;
    }

    public ILogger<CodeUpdatedLogEventProcessor> Logger { get; set; }

    public override Task<InterestedEvent> GetInterestedEventAsync(IChainContext chainContext)
    {
        if (InterestedEvent != null)
            return Task.FromResult(InterestedEvent);

        var address = _smartContractAddressService.GetZeroSmartContractAddress();
        if (address == null) return null;

        InterestedEvent = GetInterestedEvent<CodeUpdated>(address);

        return Task.FromResult(InterestedEvent);
    }

    protected override async Task ProcessLogEventAsync(Block block, LogEvent logEvent)
    {
        var eventData = new CodeUpdated();
        eventData.MergeFrom(logEvent);

        var chainContext = new ChainContext
        {
            BlockHash = block.GetHash(),
            BlockHeight = block.Height
        };
        var smartContractRegistration =
            await _smartContractRegistrationInStateProvider.GetSmartContractRegistrationAsync(chainContext,
                eventData.Address);
        await _smartContractRegistrationProvider.SetSmartContractRegistrationAsync(chainContext, eventData.Address,
            smartContractRegistration);
        _smartContractExecutiveService.CleanExecutive(eventData.Address);

        Logger.LogDebug($"Updated contract {eventData}");
    }
}