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
    public class IrreversibleBlockHeightUnacceptableLogEventProcessor : LogEventProcessorBase,
        IBlockAcceptedLogEventProcessor
    {
        private readonly ITransactionPackingOptionProvider _transactionPackingOptionProvider;
        private readonly ISmartContractAddressService _smartContractAddressService;

        public ILogger<IrreversibleBlockHeightUnacceptableLogEventProcessor> Logger { get; set; }

        public IrreversibleBlockHeightUnacceptableLogEventProcessor(
            ITransactionPackingOptionProvider transactionPackingOptionProvider,
            ISmartContractAddressService smartContractAddressService)
        {
            _transactionPackingOptionProvider = transactionPackingOptionProvider;
            _smartContractAddressService = smartContractAddressService;

            Logger = NullLogger<IrreversibleBlockHeightUnacceptableLogEventProcessor>.Instance;
        }

        public override async Task<InterestedEvent> GetInterestedEventAsync(IChainContext chainContext)
        {
            if (InterestedEvent != null)
                return InterestedEvent;
            var smartContractAddressDto = await _smartContractAddressService.GetSmartContractAddressAsync(
                chainContext, ConsensusSmartContractAddressNameProvider.StringName);
            if (smartContractAddressDto == null) return null;

            var interestedEvent =
                GetInterestedEvent<IrreversibleBlockHeightUnacceptable>(smartContractAddressDto.SmartContractAddress
                    .Address);
            if (!smartContractAddressDto.Irreversible) return interestedEvent;

            InterestedEvent = interestedEvent;
            return InterestedEvent;
        }

        protected override Task ProcessLogEventAsync(Block block, LogEvent logEvent)
        {
            var distanceToLib = new IrreversibleBlockHeightUnacceptable();
            distanceToLib.MergeFrom(logEvent);

            var blockIndex = new BlockIndex
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height
            };
            if (distanceToLib.DistanceToIrreversibleBlockHeight > 0)
            {
                Logger.LogDebug($"Distance to lib height: {distanceToLib.DistanceToIrreversibleBlockHeight}");
                _transactionPackingOptionProvider.SetTransactionPackingOptionAsync(blockIndex, false);
            }
            else
            {
                _transactionPackingOptionProvider.SetTransactionPackingOptionAsync(blockIndex, true);
            }

            return Task.CompletedTask;
        }
    }
}