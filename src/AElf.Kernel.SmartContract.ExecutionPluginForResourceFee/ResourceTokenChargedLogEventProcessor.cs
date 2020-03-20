using System.Linq;
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
        private readonly ITotalResourceTokensMapsProvider _totalTotalResourceTokensMapsProvider;
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
            ITotalResourceTokensMapsProvider totalTotalResourceTokensMapsProvider)
        {
            _smartContractAddressService = smartContractAddressService;
            _totalTotalResourceTokensMapsProvider = totalTotalResourceTokensMapsProvider;
            Logger = NullLogger<ResourceTokenChargedLogEventProcessor>.Instance;
        }

        public async Task ProcessAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
        {
            var eventData = new ResourceTokenCharged();
            eventData.MergeFrom(logEvent);
            if (eventData.Symbol == null || eventData.Amount == 0)
                return;

            // TODO: Get -> Modify -> Set is slow, consider collect all logEvents then generate the totalResourceTokensMap at once.
            var totalResourceTokensMaps =
                await _totalTotalResourceTokensMapsProvider.GetTotalResourceTokensMapsAsync(
                    new ChainContext
                    {
                        BlockHash = block.GetHash(),
                        BlockHeight = block.Height,
                    }) ?? new TotalResourceTokensMaps();

            if (totalResourceTokensMaps.Value.Any() &&
                totalResourceTokensMaps.Value.Any(b => b.ContractAddress == eventData.ContractAddress))
            {
                var oldBill = totalResourceTokensMaps.Value.First(b => b.ContractAddress == eventData.ContractAddress);
                if (oldBill.TokensMap.Value.ContainsKey(eventData.Symbol))
                {
                    oldBill.TokensMap.Value[eventData.Symbol] += eventData.Amount;
                }
                else
                {
                    oldBill.TokensMap.Value.Add(eventData.Symbol, eventData.Amount);
                }
            }
            else
            {
                var contractTotalResourceTokens = new ContractTotalResourceTokens
                {
                    ContractAddress = eventData.ContractAddress,
                    TokensMap = new TotalResourceTokensMap
                    {
                        Value =
                        {
                            {eventData.Symbol, eventData.Amount}
                        }
                    }
                };
                totalResourceTokensMaps.Value.Add(contractTotalResourceTokens);
            }

            await _totalTotalResourceTokensMapsProvider.SetTotalResourceTokensMapsAsync(new BlockIndex
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height
            }, totalResourceTokensMaps);
        }
    }
}