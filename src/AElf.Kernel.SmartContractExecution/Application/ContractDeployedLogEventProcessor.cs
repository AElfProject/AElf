using System.Threading.Tasks;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Events;
using AElf.Standards.ACS0;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.EventBus.Local;

namespace AElf.Kernel.SmartContractExecution.Application;

public class ContractDeployedLogEventProcessor : LogEventProcessorBase, IBlockAcceptedLogEventProcessor
{
    private readonly ISmartContractAddressService _smartContractAddressService;
    public ILocalEventBus LocalEventBus { get; set; }

    public ContractDeployedLogEventProcessor(ISmartContractAddressService smartContractAddressService)
    {
        _smartContractAddressService = smartContractAddressService;
        Logger = NullLogger<ContractDeployedLogEventProcessor>.Instance;
        LocalEventBus = NullLocalEventBus.Instance;
    }

    public ILogger<ContractDeployedLogEventProcessor> Logger { get; set; }
   


    public override Task<InterestedEvent> GetInterestedEventAsync(IChainContext chainContext)
    {
        if (InterestedEvent != null)
            return Task.FromResult(InterestedEvent);

        var address = _smartContractAddressService.GetZeroSmartContractAddress();
        if (address == null) return null;

        InterestedEvent = GetInterestedEvent<ContractDeployed>(address);

        return Task.FromResult(InterestedEvent);
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