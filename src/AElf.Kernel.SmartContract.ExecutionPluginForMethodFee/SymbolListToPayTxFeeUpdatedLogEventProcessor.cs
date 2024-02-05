using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Events;
using AElf.Kernel.Token;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee;

internal class SymbolListToPayTxFeeUpdatedLogEventProcessor : LogEventProcessorBase, IBlockAcceptedLogEventProcessor
{
    private readonly ISmartContractAddressService _smartContractAddressService;
    private ILocalEventBus LocalEventBus { get; set; }

    public SymbolListToPayTxFeeUpdatedLogEventProcessor(ISmartContractAddressService smartContractAddressService)
    {
        _smartContractAddressService = smartContractAddressService;
        Logger = NullLogger<SymbolListToPayTxFeeUpdatedLogEventProcessor>.Instance;
        LocalEventBus = NullLocalEventBus.Instance;
    }

    private ILogger<SymbolListToPayTxFeeUpdatedLogEventProcessor> Logger { get; }

    public override async Task<InterestedEvent> GetInterestedEventAsync(IChainContext chainContext)
    {
        if (InterestedEvent != null)
            return InterestedEvent;

        var smartContractAddressDto = await _smartContractAddressService.GetSmartContractAddressAsync(
            chainContext, TokenSmartContractAddressNameProvider.StringName);
        if (smartContractAddressDto == null) return null;

        var interestedEvent =
            GetInterestedEvent<ExtraTokenListModified>(smartContractAddressDto.SmartContractAddress.Address);
        if (!smartContractAddressDto.Irreversible) return interestedEvent;
        InterestedEvent = interestedEvent;

        return InterestedEvent;
    }

    protected override async Task ProcessLogEventAsync(Block block, LogEvent logEvent)
    {
        await LocalEventBus.PublishAsync(new LogEventDataEvent
        {
            Block = block,
            LogEvent = logEvent
        });
    }
}