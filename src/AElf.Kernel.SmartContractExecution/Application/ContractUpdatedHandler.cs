using System.Threading.Tasks;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.SmartContract.Application;
using AElf.Standards.ACS0;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.SmartContractExecution.Application;

public class ContractUpdatedHandler : LogEventContextHandler
{
    private readonly ISmartContractAddressService _smartContractAddressService;
    private readonly ISmartContractExecutiveService _smartContractExecutiveService;
    public ILogger<ContractUpdatedHandler> Logger { get; set; }

    public ContractUpdatedHandler(
        ISmartContractAddressService smartContractAddressService,
        ISmartContractRegistrationProvider smartContractRegistrationProvider,
        ISmartContractRegistrationInStateProvider smartContractRegistrationInStateProvider,
        ISmartContractExecutiveService smartContractExecutiveService) : base(
        smartContractRegistrationProvider,
        smartContractRegistrationInStateProvider)
    {
        _smartContractAddressService = smartContractAddressService;
        _smartContractExecutiveService = smartContractExecutiveService;
        Logger = NullLogger<ContractUpdatedHandler>.Instance;
    }

    protected override bool IsInterestedAsync(LogEvent logEvent)
    {
        var address = _smartContractAddressService.GetZeroSmartContractAddress();
        if (address == null) return false;
        if (logEvent.Address.Equals(address) && logEvent.Name.Equals(nameof(CodeUpdated)))
            return true;
        return false;
    }

    protected override async Task ProcessLogEventAsync(ChainContext chainContext, LogEvent logEvent)
    {
        var codeUpdated = new CodeUpdated();
        codeUpdated.MergeFrom(logEvent);
        await SetSmartContractRegistrationAsync(chainContext, codeUpdated.Address);
        _smartContractExecutiveService.CleanExecutive(codeUpdated.Address);
        Logger.LogDebug($"Updated contract {codeUpdated}");
    }
}