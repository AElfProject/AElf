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

namespace AElf.Kernel.SmartContract.ExecutionPluginForResourceFee
{
    public class ResourceTokenChargedLogEventProcessor : LogEventProcessorBase, IBlockAcceptedLogEventProcessor
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITotalResourceTokensMapsProvider _totalTotalResourceTokensMapsProvider;
        private ILogger<ResourceTokenChargedLogEventProcessor> Logger { get; set; }

        public ResourceTokenChargedLogEventProcessor(ISmartContractAddressService smartContractAddressService,
            ITotalResourceTokensMapsProvider totalTotalResourceTokensMapsProvider)
        {
            _smartContractAddressService = smartContractAddressService;
            _totalTotalResourceTokensMapsProvider = totalTotalResourceTokensMapsProvider;
            Logger = NullLogger<ResourceTokenChargedLogEventProcessor>.Instance;
        }

        public override async Task<InterestedEvent> GetInterestedEventAsync(IChainContext chainContext)
        {
            if (InterestedEvent != null)
                return InterestedEvent;

            var smartContractAddressDto = await _smartContractAddressService.GetSmartContractAddressAsync(
                chainContext, TokenSmartContractAddressNameProvider.StringName);
            if (smartContractAddressDto == null) return null;

            var interestedEvent =
                GetInterestedEvent<ResourceTokenCharged>(smartContractAddressDto.SmartContractAddress.Address);

            if (!smartContractAddressDto.Irreversible) return interestedEvent;
            
            InterestedEvent = interestedEvent;

            return InterestedEvent;
        }

        public override async Task ProcessAsync(Block block, Dictionary<TransactionResult, List<LogEvent>> logEventsMap)
        {
            var blockHash = block.GetHash();
            var blockHeight = block.Height;
            var totalResourceTokensMaps = new TotalResourceTokensMaps
            {
                BlockHash = blockHash,
                BlockHeight = blockHeight
            };

            foreach (var logEvent in logEventsMap.Values.SelectMany(logEvents => logEvents))
            {
                var eventData = new ResourceTokenCharged();
                eventData.MergeFrom(logEvent);
                if (eventData.Symbol == null || eventData.Amount == 0)
                    continue;

                if (totalResourceTokensMaps.Value.Any(b => b.ContractAddress == eventData.ContractAddress))
                {
                    var oldBill =
                        totalResourceTokensMaps.Value.First(b => b.ContractAddress == eventData.ContractAddress);
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
            }

            await _totalTotalResourceTokensMapsProvider.SetTotalResourceTokensMapsAsync(new BlockIndex
            {
                BlockHash = blockHash,
                BlockHeight = blockHeight
            }, totalResourceTokensMaps);
        }
    }
}