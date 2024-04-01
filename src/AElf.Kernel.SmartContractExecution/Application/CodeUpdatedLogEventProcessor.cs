using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Events;
using AElf.Standards.ACS0;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContractExecution.Application;

public class CodeUpdatedLogEventProcessor : LogEventProcessorBase, IBlockAcceptedLogEventProcessor
{
    private readonly ISmartContractAddressService _smartContractAddressService;
    public ILocalEventBus LocalEventBus { get; set; }

    public CodeUpdatedLogEventProcessor(ISmartContractAddressService smartContractAddressService)
    {
        _smartContractAddressService = smartContractAddressService;
        Logger = NullLogger<CodeUpdatedLogEventProcessor>.Instance;
        LocalEventBus = NullLocalEventBus.Instance;
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
        await LocalEventBus.PublishAsync(new LogEventContextData
        {
            Block = block,
            LogEvent = logEvent
        });
    }
}