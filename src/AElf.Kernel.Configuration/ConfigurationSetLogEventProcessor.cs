using System.Threading.Tasks;
using AElf.Contracts.Configuration;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.Kernel.Configuration;

public class ConfigurationSetLogEventProcessor : LogEventProcessorBase, IBlockAcceptedLogEventProcessor
{
    private readonly IConfigurationService _configurationService;
    private readonly ISmartContractAddressService _smartContractAddressService;

    public ConfigurationSetLogEventProcessor(ISmartContractAddressService smartContractAddressService,
        IConfigurationService configurationService)
    {
        _smartContractAddressService = smartContractAddressService;
        _configurationService = configurationService;
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
        var configurationSet = new ConfigurationSet();
        configurationSet.MergeFrom(logEvent);

        await _configurationService.ProcessConfigurationAsync(configurationSet.Key, configurationSet.Value,
            new BlockIndex
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height
            });
    }
}