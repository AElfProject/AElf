using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee
{
    internal class TransactionFeeChargedLogEventProcessor : LogEventProcessorBase, IBlockAcceptedLogEventProcessor
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITotalTransactionFeesMapProvider _totalTransactionFeesMapProvider;
        private ILogger<TransactionFeeChargedLogEventProcessor> Logger { get; set; }

        public TransactionFeeChargedLogEventProcessor(ISmartContractAddressService smartContractAddressService,
            ITotalTransactionFeesMapProvider totalTransactionFeesMapProvider)
        {
            _smartContractAddressService = smartContractAddressService;
            _totalTransactionFeesMapProvider = totalTransactionFeesMapProvider;
            Logger = NullLogger<TransactionFeeChargedLogEventProcessor>.Instance;
        }

        public override async Task<InterestedEvent> GetInterestedEventAsync(IChainContext chainContext)
        {
            if (InterestedEvent != null)
                return InterestedEvent;

            var smartContractAddressDto = await _smartContractAddressService.GetSmartContractAddressAsync(
                chainContext, TokenSmartContractAddressNameProvider.StringName);

            if (smartContractAddressDto == null) return null;

            var interestedEvent =
                GetInterestedEvent<TransactionFeeCharged>(smartContractAddressDto.SmartContractAddress.Address);
            if (!smartContractAddressDto.Irreversible) return interestedEvent;

            InterestedEvent = interestedEvent;

            return InterestedEvent;
        }

        public override async Task ProcessAsync(Block block, Dictionary<TransactionResult, List<LogEvent>> logEventsMap)
        {
            var blockHash = block.GetHash();
            var blockHeight = block.Height;
            var totalTxFeesMap = new TotalTransactionFeesMap
            {
                BlockHash = blockHash,
                BlockHeight = blockHeight
            };

            foreach (var logEvent in logEventsMap.Values.SelectMany(logEvents => logEvents))
            {
                var eventData = new TransactionFeeCharged();
                eventData.MergeFrom(logEvent);
                if (eventData.Symbol == null || eventData.Amount == 0)
                    continue;

                if (totalTxFeesMap.Value.ContainsKey(eventData.Symbol))
                {
                    totalTxFeesMap.Value[eventData.Symbol] += eventData.Amount;
                }
                else
                {
                    totalTxFeesMap.Value[eventData.Symbol] = eventData.Amount;
                }
            }

            if (totalTxFeesMap.Value.Any()) // for some TransactionFeeCharged event with 0 fee to charge.
            {
                await _totalTransactionFeesMapProvider.SetTotalTransactionFeesMapAsync(new BlockIndex
                {
                    BlockHash = blockHash,
                    BlockHeight = blockHeight
                }, totalTxFeesMap);
            }
        }
    }
}