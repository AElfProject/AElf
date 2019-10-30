using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.Blockchain.Application;
using AElf.Kernel.Blockchain.Events;
using AElf.Kernel.TransactionPool.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    /// <summary>
    /// Discover LIB from consensus contract then set LIB.
    /// </summary>
    internal class
        BestChainFoundEventHandlerForIrreversibleBlockHeightUnacceptable : ILocalEventHandler<BestChainFoundEventData>,
            ITransientDependency
    {
        private readonly ITransactionInclusivenessProvider _transactionInclusivenessProvider;

        private readonly ContractEventDiscoveryService<IrreversibleBlockHeightUnacceptable>
            _unacceptableLibHeightEventDiscoveryService;

        private readonly ISmartContractAddressService _smartContractAddressService;

        public ILogger<BestChainFoundEventHandlerForIrreversibleBlockHeightUnacceptable> Logger { get; set; }

        public BestChainFoundEventHandlerForIrreversibleBlockHeightUnacceptable(
            ITransactionInclusivenessProvider transactionInclusivenessProvider,
            ContractEventDiscoveryService<IrreversibleBlockHeightUnacceptable>
                unacceptableLibHeightEventDiscoveryService,
            ISmartContractAddressService smartContractAddressService)
        {
            _transactionInclusivenessProvider = transactionInclusivenessProvider;
            _unacceptableLibHeightEventDiscoveryService = unacceptableLibHeightEventDiscoveryService;
            _smartContractAddressService = smartContractAddressService;

            Logger = NullLogger<BestChainFoundEventHandlerForIrreversibleBlockHeightUnacceptable>.Instance;
        }

        public async Task HandleEventAsync(BestChainFoundEventData eventData)
        {
            Logger.LogDebug(
                $"Handle best chain found for lib in {nameof(BestChainFoundEventHandlerForIrreversibleBlockHeightUnacceptable)}: " +
                $"BlockHeight: {eventData.BlockHeight}, BlockHash: {eventData.BlockHash}");

            var consensusAddress =
                _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider.Name);

            try
            {
                await HandleUnacceptableLibHeightEvent(eventData, consensusAddress);
            }
            catch (Exception e)
            {
                Logger.LogError(
                    $"Error while executing {nameof(HandleUnacceptableLibHeightEvent)}.",
                    e);
                throw;
            }
        }

        private async Task HandleUnacceptableLibHeightEvent(BestChainFoundEventData eventData, Address consensusAddress)
        {
            var distanceToLib =
                (await _unacceptableLibHeightEventDiscoveryService.GetEventMessagesAsync(eventData.BlockHash,
                    consensusAddress))
                .FirstOrDefault();
            if (distanceToLib != null && distanceToLib.DistanceToIrreversibleBlockHeight > 0)
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