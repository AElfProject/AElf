using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.FeeCalculation.Extensions;
using AElf.Kernel.FeeCalculation.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.Events;
using AElf.Kernel.Token;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace AElf.Kernel.FeeCalculation.Application;

public class LogEventContextHandler : ILocalEventHandler<LogEventContextData>, ITransientDependency
{
    private readonly ICalculateFunctionProvider _calculateFunctionProvider;
    private readonly ISmartContractAddressService _smartContractAddressService;

    public LogEventContextHandler(ICalculateFunctionProvider calculateFunctionProvider,
        ISmartContractAddressService smartContractAddressService)
    {
        _calculateFunctionProvider = calculateFunctionProvider;
        _smartContractAddressService = smartContractAddressService;
    }

    public async Task HandleEventAsync(LogEventContextData eventContextData)
    {
        var smartContractAddressDto = await _smartContractAddressService.GetSmartContractAddressAsync(
            new ChainContext
            {
                BlockHash = eventContextData.Block.GetHash(),
                BlockHeight = eventContextData.Block.Height
            }, TokenSmartContractAddressNameProvider.StringName);
        if (smartContractAddressDto == null) return;
        if (eventContextData.LogEvent.Address != smartContractAddressDto.SmartContractAddress.Address ||
            eventContextData.LogEvent.Name != nameof(CalculateFeeAlgorithmUpdated)) return;

        var calculateFeeAlgorithmUpdated = new CalculateFeeAlgorithmUpdated();
        calculateFeeAlgorithmUpdated.MergeFrom(eventContextData.LogEvent);
        await _calculateFunctionProvider.AddCalculateFunctions(new BlockIndex
        {
            BlockHash = eventContextData.Block.GetHash(),
            BlockHeight = eventContextData.Block.Height
        }, calculateFeeAlgorithmUpdated.AllTypeFeeCoefficients.ToCalculateFunctionDictionary());
    }
}