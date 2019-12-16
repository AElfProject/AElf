using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.Txn.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    public class IrreversibleBlockHeightUnacceptableLogEventHandler : IBestChainFoundLogEventHandler
    {
        private readonly ITransactionPackingService _transactionPackingService;
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

        public ILogger<IrreversibleBlockHeightUnacceptableLogEventHandler> Logger { get; set; }

        public IrreversibleBlockHeightUnacceptableLogEventHandler(
            ITransactionPackingService transactionPackingService,
            ISmartContractAddressService smartContractAddressService)
        {
            _transactionPackingService = transactionPackingService;
            _smartContractAddressService = smartContractAddressService;

            Logger = NullLogger<IrreversibleBlockHeightUnacceptableLogEventHandler>.Instance;
        }

        public async Task HandleAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
        {
            var distanceToLib = new IrreversibleBlockHeightUnacceptable();
            distanceToLib.MergeFrom(logEvent);

            if (distanceToLib.DistanceToIrreversibleBlockHeight > 0)
            {
                Logger.LogDebug($"Distance to lib height: {distanceToLib.DistanceToIrreversibleBlockHeight}");
                _transactionPackingService.DisableTransactionPacking();
            }
            else
            {
                _transactionPackingService.EnableTransactionPacking();
            }

            await Task.CompletedTask;
        }
    }
}