using System.Threading.Tasks;
using AElf.Contracts.Consensus.AEDPoS;
using AElf.Kernel.SmartContract.Application;
using AElf.CSharp.Core.Extension;
using AElf.Types;

namespace AElf.Kernel.Consensus.AEDPoS.Application
{
    internal class SecretSharingInformationLogEventProcessor : LogEventProcessorBase, IBlocksExecutionSucceededLogEventProcessor
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ISecretSharingService _secretSharingService;

        public SecretSharingInformationLogEventProcessor(
            ISmartContractAddressService smartContractAddressService,
            ISecretSharingService secretSharingService)
        {
            _smartContractAddressService = smartContractAddressService;
            _secretSharingService = secretSharingService;
        }

        public override async Task<InterestedEvent> GetInterestedEventAsync(IChainContext chainContext)
        {
            if (InterestedEvent != null) return InterestedEvent;
            var smartContractAddressDto = await _smartContractAddressService.GetSmartContractAddressAsync(
                chainContext, ConsensusSmartContractAddressNameProvider.StringName);
            if (smartContractAddressDto == null) return null;
            
            var interestedEvent =
                GetInterestedEvent<SecretSharingInformation>(smartContractAddressDto.SmartContractAddress.Address);
            if (!smartContractAddressDto.Irreversible) return interestedEvent;
            
            InterestedEvent = interestedEvent;
            return InterestedEvent;
        }

        protected override async Task ProcessLogEventAsync(Block block, LogEvent logEvent)
        {
            var secretSharingInformation = new SecretSharingInformation();
            secretSharingInformation.MergeFrom(logEvent);
            await _secretSharingService.AddSharingInformationAsync(secretSharingInformation);
        }
    }
}