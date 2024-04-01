using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Events;
using AElf.Kernel.Token;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.FeeCalculation.Application;

public class TransactionFeeCalculatorCoefficientUpdatedLogEventProcessor : LogEventProcessorBase,
    IBlockAcceptedLogEventProcessor
{
    public ILocalEventBus LocalEventBus { get; set; }
    private readonly ISmartContractAddressService _smartContractAddressService;

    public TransactionFeeCalculatorCoefficientUpdatedLogEventProcessor(
        ISmartContractAddressService smartContractAddressService)
    {
        _smartContractAddressService = smartContractAddressService;
        LocalEventBus = NullLocalEventBus.Instance;
        Logger = NullLogger<TransactionFeeCalculatorCoefficientUpdatedLogEventProcessor>.Instance;
    }

    private ILogger<TransactionFeeCalculatorCoefficientUpdatedLogEventProcessor> Logger { get; }

    public override async Task<InterestedEvent> GetInterestedEventAsync(IChainContext chainContext)
    {
        if (InterestedEvent != null)
            return InterestedEvent;

        var smartContractAddressDto = await _smartContractAddressService.GetSmartContractAddressAsync(
            chainContext, TokenSmartContractAddressNameProvider.StringName);

        if (smartContractAddressDto == null) return null;
        var interestedEvent =
            GetInterestedEvent<CalculateFeeAlgorithmUpdated>(smartContractAddressDto.SmartContractAddress.Address);

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