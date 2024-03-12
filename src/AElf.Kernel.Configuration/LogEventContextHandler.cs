using System.Threading.Tasks;
using AElf.Contracts.Configuration;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Events;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.Configuration;

public class LogEventContextHandler : ILocalEventHandler<LogEventContextData>, ITransientDependency
{
    private readonly IConfigurationService _configurationService;
    private readonly ISmartContractAddressService _smartContractAddressService;

    public LogEventContextHandler(ISmartContractAddressService smartContractAddressService,
        IConfigurationService configurationService)
    {
        _smartContractAddressService = smartContractAddressService;
        _configurationService = configurationService;
    }

    public async Task HandleEventAsync(LogEventContextData eventContextData)
    {
        var smartContractAddressDto = await _smartContractAddressService.GetSmartContractAddressAsync(
            new ChainContext
            {
                BlockHash = eventContextData.Block.GetHash(),
                BlockHeight = eventContextData.Block.Height
            }, ConfigurationSmartContractAddressNameProvider.StringName);
        if (smartContractAddressDto == null) return;
        if (eventContextData.LogEvent.Address != smartContractAddressDto.SmartContractAddress.Address ||
            eventContextData.LogEvent.Name != nameof(ConfigurationSet)) return;
        
        var configurationSet = new ConfigurationSet();
        configurationSet.MergeFrom(eventContextData.LogEvent);
        await _configurationService.ProcessConfigurationAsync(configurationSet.Key, configurationSet.Value,
            new BlockIndex
            {
                BlockHash = eventContextData.Block.GetHash(),
                BlockHeight = eventContextData.Block.Height
            });
    }
}