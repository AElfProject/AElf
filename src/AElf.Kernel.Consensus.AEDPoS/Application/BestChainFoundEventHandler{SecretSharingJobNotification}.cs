using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Cryptography.SecretSharing;
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
        BestChainFoundEventHandlerForIrreversibleSecretSharingJobNotification :
            ILocalEventHandler<BestChainFoundEventData>, ITransientDependency
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ContractEventDiscoveryService<SecretSharingJobNotification> _secretSharingDiscoveryService;
        private readonly ITaskQueueManager _taskQueueManager;

        public ILogger<BestChainFoundEventHandlerForIrreversibleSecretSharingJobNotification> Logger { get; set; }

        public BestChainFoundEventHandlerForIrreversibleSecretSharingJobNotification(
            ISmartContractAddressService smartContractAddressService,
            ContractEventDiscoveryService<SecretSharingJobNotification> secretSharingDiscoveryService,
            ITaskQueueManager taskQueueManager)
        {
            _smartContractAddressService = smartContractAddressService;
            _secretSharingDiscoveryService = secretSharingDiscoveryService;
            _taskQueueManager = taskQueueManager;

            Logger = NullLogger<BestChainFoundEventHandlerForIrreversibleSecretSharingJobNotification>.Instance;
        }

        public async Task HandleEventAsync(BestChainFoundEventData eventData)
        {
            Logger.LogDebug(
                $"Handle best chain found for lib in {nameof(BestChainFoundEventHandlerForIrreversibleSecretSharingJobNotification)}: " +
                $"BlockHeight: {eventData.BlockHeight}, BlockHash: {eventData.BlockHash}");

            var consensusAddress =
                _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider.Name);

            try
            {
                await HandleSecretSharingEvent(eventData, consensusAddress);
            }
            catch (Exception e)
            {
                Logger.LogError(
                    $"Error while executing {nameof(HandleSecretSharingEvent)}.",
                    e);
                throw;
            }
        }

        private async Task HandleSecretSharingEvent(BestChainFoundEventData eventData, Address consensusAddress)
        {
            var notification =
                await _secretSharingDiscoveryService.GetEventMessagesAsync(eventData.BlockHash, consensusAddress);
            _taskQueueManager.Enqueue(
                async () =>
                {
                    SecretSharingHelper.EncodeSecret()
                });
        }
    }
}