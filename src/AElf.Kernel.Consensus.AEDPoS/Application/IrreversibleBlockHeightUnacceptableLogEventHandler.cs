using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    public class IrreversibleBlockHeightUnacceptableLogEventHandler : ILogEventHandler
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private ITransactionInclusivenessProvider _transactionInclusivenessProvider;

        private LogEvent _interestedEvent;

        public IrreversibleBlockHeightUnacceptableLogEventHandler(
            ISmartContractAddressService smartContractAddressService,
            ITransactionInclusivenessProvider transactionInclusivenessProvider)
        {
            _smartContractAddressService = smartContractAddressService;
            _transactionInclusivenessProvider = transactionInclusivenessProvider;
        }

        public ILogger<IrreversibleBlockHeightUnacceptableLogEventHandler> Logger { get; set; }

        public LogEvent InterestedEvent
        {
            get
            {
                if (_interestedEvent != null) return _interestedEvent;
                var address =
                    _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider
                        .Name);
                _interestedEvent = new IrreversibleBlockHeightUnacceptable().ToLogEvent(address);
                return _interestedEvent;
            }
        }

        public async Task Handle(Block block, TransactionResult result, LogEvent log)
        {
            var distanceToLib = new IrreversibleBlockHeightUnacceptable();
            distanceToLib.MergeFrom(log);
            if (distanceToLib.DistanceToIrreversibleBlockHeight > 0)
            {
                Logger.LogDebug($"Distance to lib height: {distanceToLib.DistanceToIrreversibleBlockHeight}");
                _transactionInclusivenessProvider.IsTransactionPackable = false;
            }
            else
            {
                _transactionInclusivenessProvider.IsTransactionPackable = true;
            }
        }
    }
}