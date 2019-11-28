using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    public class SecretSharingInformationLogEventHandler : IBestChainFoundLogEventHandler
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ISecretSharingService _secretSharingService;

        private LogEvent _interestedEvent;

        public SecretSharingInformationLogEventHandler(
            ISmartContractAddressService smartContractAddressService,
            ISecretSharingService secretSharingService)
        {
            _smartContractAddressService = smartContractAddressService;
            _secretSharingService = secretSharingService;
        }

        public ILogger<SecretSharingInformationLogEventHandler> Logger { get; set; }

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

        public Task HandleAsync(Block block, TransactionResult result, LogEvent logEvent)
        {
            _secretSharingService.AddSharingInformationAsync(logEvent);
            return Task.CompletedTask;
        }
    }
}