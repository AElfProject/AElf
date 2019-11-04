using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Microsoft.Extensions.Logging;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    internal class SecretSharingJobNotificationLogEventHandler : ILogEventHandler
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ISecretSharingService _secretSharingService;

        private LogEvent _interestedEvent;

        public SecretSharingJobNotificationLogEventHandler(
            ISmartContractAddressService smartContractAddressService,
            ISecretSharingService secretSharingService)
        {
            _smartContractAddressService = smartContractAddressService;
            _secretSharingService = secretSharingService;
        }

        public ILogger<SecretSharingJobNotificationLogEventHandler> Logger { get; set; }

        public LogEvent InterestedEvent
        {
            get
            {
                if (_interestedEvent != null) return _interestedEvent;
                var address =
                    _smartContractAddressService.GetAddressByContractName(ConsensusSmartContractAddressNameProvider
                        .Name);
                _interestedEvent = new SecretSharingInformation().ToLogEvent(address);
                return _interestedEvent;
            }
        }

        public Task Handle(Block block, TransactionResult result, LogEvent log)
        {
            var secretSharingInformation = new SecretSharingInformation();
            secretSharingInformation.MergeFrom(log);
            _secretSharingService.AddSharingInformationAsync(secretSharingInformation);
            return Task.CompletedTask;
        }
    }
}