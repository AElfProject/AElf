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
    public class TransactionFeeChargedLogEventProcessor : IBlockAcceptedLogEventProcessor
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITotalTransactionFeesMapProvider _totalTransactionFeesMapProvider;
        private LogEvent _interestedEvent;
        private ILogger<TransactionFeeChargedLogEventProcessor> Logger { get; set; }

        public LogEvent InterestedEvent
        {
            get
            {
                if (_interestedEvent != null)
                    return _interestedEvent;

                var address =
                    _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name);

                _interestedEvent = new TransactionFeeCharged().ToLogEvent(address);

                return _interestedEvent;
            }
        }

        public TransactionFeeChargedLogEventProcessor(ISmartContractAddressService smartContractAddressService,
            ITotalTransactionFeesMapProvider totalTransactionFeesMapProvider)
        {
            _smartContractAddressService = smartContractAddressService;
            _totalTransactionFeesMapProvider = totalTransactionFeesMapProvider;
            Logger = NullLogger<TransactionFeeChargedLogEventProcessor>.Instance;
        }

        public async Task ProcessAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
        {
            var eventData = new TransactionFeeCharged();
            eventData.MergeFrom(logEvent);
            if (eventData.Symbol == null || eventData.Amount == 0)
                return;

            // TODO: Get -> Modify -> Set is slow, consider collect all logEvents then generate the totalTxFeesMap at once.
            var totalTxFeesMap = await _totalTransactionFeesMapProvider.GetTotalTransactionFeesMapAsync(new ChainContext
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height,
            }) ?? new TotalTransactionFeesMap();

            if (totalTxFeesMap.Value.ContainsKey(eventData.Symbol))
            {
                totalTxFeesMap.Value[eventData.Symbol] = totalTxFeesMap.Value[eventData.Symbol] + eventData.Amount;
            }
            else
            {
                totalTxFeesMap.Value[eventData.Symbol] = eventData.Amount;
            }

            await _totalTransactionFeesMapProvider.SetTotalTransactionFeesMapAsync(new BlockIndex
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height
            }, totalTxFeesMap);
        }
    }
}