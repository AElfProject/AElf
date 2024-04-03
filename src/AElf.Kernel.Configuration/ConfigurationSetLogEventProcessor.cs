using System.Threading.Tasks;
using AElf.Contracts.Configuration;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Events;
using AElf.Types;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.Configuration;

public class ConfigurationSetLogEventProcessor : LogEventProcessorBase, IBlockAcceptedLogEventProcessor
{
    private readonly ISmartContractAddressService _smartContractAddressService;
    public ILocalEventBus LocalEventBus { get; set; }

    public ConfigurationSetLogEventProcessor(ISmartContractAddressService smartContractAddressService)
    {
        _smartContractAddressService = smartContractAddressService;
        LocalEventBus = NullLocalEventBus.Instance;
    }

    public override async Task<InterestedEvent> GetInterestedEventAsync(IChainContext chainContext)
    {
        if (InterestedEvent != null)
            return InterestedEvent;

        var smartContractAddressDto = await _smartContractAddressService.GetSmartContractAddressAsync(
            chainContext, ConfigurationSmartContractAddressNameProvider.StringName);

        if (smartContractAddressDto == null) return null;

        var interestedEvent =
            GetInterestedEvent<ConfigurationSet>(smartContractAddressDto.SmartContractAddress.Address);

        if (!smartContractAddressDto.Irreversible) return interestedEvent;

        InterestedEvent = interestedEvent;

        return InterestedEvent;
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