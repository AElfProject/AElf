using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.SmartContract.ExecutionPluginForResourceFee
{
    public class ResourceTokenChargedLogEventProcessor : IBlockAcceptedLogEventProcessor
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITotalResourceTokensMapProvider _totalTotalResourceTokensMapProvider;
        private LogEvent _interestedEvent;
        private ILogger<ResourceTokenChargedLogEventProcessor> Logger { get; set; }

        public LogEvent InterestedEvent
        {
            get
            {
                if (_interestedEvent != null)
                    return _interestedEvent;

                var address =
                    _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name);

                _interestedEvent = new ResourceTokenCharged().ToLogEvent(address);

                return _interestedEvent;
            }
        }

        public ResourceTokenChargedLogEventProcessor(ISmartContractAddressService smartContractAddressService,
            ITotalResourceTokensMapProvider totalTotalResourceTokensMapProvider)
        {
            _smartContractAddressService = smartContractAddressService;
            _totalTotalResourceTokensMapProvider = totalTotalResourceTokensMapProvider;
            Logger = NullLogger<ResourceTokenChargedLogEventProcessor>.Instance;
        }

        public async Task ProcessAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
        {
            var eventData = new ResourceTokenCharged();
            eventData.MergeFrom(logEvent);
            if (eventData.Symbol == null || eventData.Amount == 0)
                return;

            // TODO: Get -> Modify -> Set is slow, consider collect all logEvents then generate the totalResourceTokensMap at once.
            var totalResourceTokensMap = await _totalTotalResourceTokensMapProvider.GetTotalResourceTokensMapAsync(new ChainContext
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height,
            });

            if (totalResourceTokensMap.Value.ContainsKey(eventData.Symbol))
            {
                totalResourceTokensMap.Value[eventData.Symbol] = totalResourceTokensMap.Value[eventData.Symbol] + eventData.Amount;
            }
            else
            {
                totalResourceTokensMap.Value[eventData.Symbol] = eventData.Amount;
            }

            await _totalTotalResourceTokensMapProvider.SetTotalResourceTokensMapAsync(new BlockIndex
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height
            }, totalResourceTokensMap);
        }
    }
}