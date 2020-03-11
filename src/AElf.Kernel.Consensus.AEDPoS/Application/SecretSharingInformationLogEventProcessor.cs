using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.SmartContract.Application;
using AElf.CSharp.Core.Extension;
using AElf.Types;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    public class SecretSharingInformationLogEventProcessor : IBestChainFoundLogEventProcessor
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ISecretSharingService _secretSharingService;

        private LogEvent _interestedEvent;

        public SecretSharingInformationLogEventProcessor(
            ISmartContractAddressService smartContractAddressService,
            ISecretSharingService secretSharingService)
        {
            _smartContractAddressService = smartContractAddressService;
            _secretSharingService = secretSharingService;
        }

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

        public Task ProcessAsync(Block block, TransactionResult result, LogEvent logEvent)
        {
            _secretSharingService.AddSharingInformationAsync(logEvent);
            return Task.CompletedTask;
        }
    }
}