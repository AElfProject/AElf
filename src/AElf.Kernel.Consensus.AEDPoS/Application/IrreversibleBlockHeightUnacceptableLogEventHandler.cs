using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.TransactionPool.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    public class IrreversibleBlockHeightUnacceptableLogEventHandler : ILogEventHandler, ISingletonDependency
    {
        private readonly ITransactionInclusivenessProvider _transactionInclusivenessProvider;
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
            ITransactionInclusivenessProvider transactionInclusivenessProvider,
            ISmartContractAddressService smartContractAddressService)
        {
            _transactionInclusivenessProvider = transactionInclusivenessProvider;
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
                _transactionInclusivenessProvider.IsTransactionPackable = false;
            }
            else
            {
                _transactionInclusivenessProvider.IsTransactionPackable = true;
            }

            await Task.CompletedTask;
        }
    }
}