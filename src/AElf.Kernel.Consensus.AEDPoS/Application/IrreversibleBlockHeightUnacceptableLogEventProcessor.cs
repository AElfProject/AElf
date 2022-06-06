using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.Consensus.AEDPoS.Application;

public class IrreversibleBlockHeightUnacceptableLogEventProcessor : LogEventProcessorBase,
    IBlockAcceptedLogEventProcessor
{
    private readonly IBlockchainService _blockchainService;
    private readonly ISmartContractAddressService _smartContractAddressService;
    private readonly ITaskQueueManager _taskQueueManager;

    public IrreversibleBlockHeightUnacceptableLogEventProcessor(
        ISmartContractAddressService smartContractAddressService,
        IBlockchainService blockchainService, ITaskQueueManager taskQueueManager)
    {
        _smartContractAddressService = smartContractAddressService;
        _blockchainService = blockchainService;
        _taskQueueManager = taskQueueManager;

        Logger = NullLogger<IrreversibleBlockHeightUnacceptableLogEventProcessor>.Instance;
    }

    public ILogger<IrreversibleBlockHeightUnacceptableLogEventProcessor> Logger { get; set; }

    public override async Task<InterestedEvent> GetInterestedEventAsync(IChainContext chainContext)
    {
        if (InterestedEvent != null)
            return InterestedEvent;
        var smartContractAddressDto = await _smartContractAddressService.GetSmartContractAddressAsync(
            chainContext, ConsensusSmartContractAddressNameProvider.StringName);
        if (smartContractAddressDto == null) return null;

        var interestedEvent =
            GetInterestedEvent<IrreversibleBlockHeightUnacceptable>(smartContractAddressDto.SmartContractAddress
                .Address);
        if (!smartContractAddressDto.Irreversible) return interestedEvent;

        InterestedEvent = interestedEvent;
        return InterestedEvent;
    }

    protected override async Task ProcessLogEventAsync(Block block, LogEvent logEvent)
    {
        var distanceToLib = new IrreversibleBlockHeightUnacceptable();
        distanceToLib.MergeFrom(logEvent);

        if (distanceToLib.DistanceToIrreversibleBlockHeight > 0)
        {
            Logger.LogDebug($"Distance to lib height: {distanceToLib.DistanceToIrreversibleBlockHeight}");
            Logger.LogDebug("Will rollback to lib height.");
            _taskQueueManager.Enqueue(
                async () =>
                {
                    var chain = await _blockchainService.GetChainAsync();
                    await _blockchainService.ResetChainToLibAsync(chain);
                }, KernelConstants.UpdateChainQueueName);
        }
    }
}