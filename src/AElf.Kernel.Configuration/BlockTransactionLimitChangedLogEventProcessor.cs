using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using AElf.Contracts.Configuration;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.Configuration
{
    public class BlockTransactionLimitChangedLogEventProcessor : IBlockAcceptedLogEventProcessor
    {
        private readonly IBlockchainStateService _blockchainStateService;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private LogEvent _interestedEvent;

        public LogEvent InterestedEvent
        {
            get
            {
                if (_interestedEvent != null)
                    return _interestedEvent;

                var address =
                    _smartContractAddressService.GetAddressByContractName(ConfigurationSmartContractAddressNameProvider
                        .Name);

                _interestedEvent = new ConfigurationSet().ToLogEvent(address);

                return _interestedEvent;
            }
        }

        public ILogger<BlockTransactionLimitChangedLogEventProcessor> Logger { get; set; }

        public BlockTransactionLimitChangedLogEventProcessor(ISmartContractAddressService smartContractAddressService,
            IBlockchainStateService blockchainStateService)
        {
            _smartContractAddressService = smartContractAddressService;
            _blockchainStateService = blockchainStateService;
            Logger = NullLogger<BlockTransactionLimitChangedLogEventProcessor>.Instance;
        }

        public async Task ProcessAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
        {
            var eventData = new ConfigurationSet();
            eventData.MergeFrom(logEvent);

            if (eventData.Key != BlockTransactionLimitConfigurationNameProvider.Name) return;

            var limit = new BlockTransactionLimit();
            limit.MergeFrom(eventData.Value);
            if (limit.Value < 0) return;
            await _blockchainStateService.AddBlockExecutedDataAsync(block.GetHash(), limit);

            Logger.LogInformation($"BlockTransactionLimit has been changed to {limit.Value}");
        }
    }
}