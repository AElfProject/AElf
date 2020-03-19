using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Txn.Application;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using AElf.CSharp.Core.Extension;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    public class IrreversibleBlockHeightUnacceptableLogEventProcessor : IBestChainFoundLogEventProcessor
    {
        private readonly TransactionPackingOptions _transactionPackingOptions;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private LogEvent _interestedEvent;

        public LogEvent InterestedEvent
        {
            get
            {
                if (_interestedEvent != null)
                    return _interestedEvent;
                var address =
                    _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider
                        .Name);
                _interestedEvent = new IrreversibleBlockHeightUnacceptable().ToLogEvent(address);
                return _interestedEvent;
            }
        }

        public ILogger<IrreversibleBlockHeightUnacceptableLogEventProcessor> Logger { get; set; }

        public IrreversibleBlockHeightUnacceptableLogEventProcessor(
            IOptionsMonitor<TransactionPackingOptions> transactionPackingOptions,
            ISmartContractAddressService smartContractAddressService)
        {
            _transactionPackingOptions = transactionPackingOptions.CurrentValue;
            _smartContractAddressService = smartContractAddressService;

            Logger = NullLogger<IrreversibleBlockHeightUnacceptableLogEventProcessor>.Instance;
        }

        public async Task ProcessAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
        {
            var distanceToLib = new IrreversibleBlockHeightUnacceptable();
            distanceToLib.MergeFrom(logEvent);

            if (distanceToLib.DistanceToIrreversibleBlockHeight > 0)
            {
                Logger.LogDebug($"Distance to lib height: {distanceToLib.DistanceToIrreversibleBlockHeight}");
                _transactionPackingOptions.IsTransactionPackable = false;
            }
            else
            {
                _transactionPackingOptions.IsTransactionPackable = true;
            }

            await Task.CompletedTask;
        }
    }
}