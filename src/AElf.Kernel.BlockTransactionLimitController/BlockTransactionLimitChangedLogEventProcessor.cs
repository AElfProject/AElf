using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using AElf.Contracts.Configuration;
using AElf.Kernel.SmartContractExecution.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.BlockTransactionLimitController
{
    public class BlockTransactionLimitChangedLogEventProcessor : IBlockAcceptedLogEventProcessor
    {
        private readonly IBlockTransactionLimitProvider _blockTransactionLimitProvider;
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

                _interestedEvent = new BlockTransactionLimitChanged().ToLogEvent(address);

                return _interestedEvent;
            }
        }

        public ILogger<BlockTransactionLimitChangedLogEventProcessor> Logger { get; set; }

        public BlockTransactionLimitChangedLogEventProcessor(IBlockTransactionLimitProvider blockTransactionLimitProvider,
            ISmartContractAddressService smartContractAddressService)
        {
            _blockTransactionLimitProvider = blockTransactionLimitProvider;
            _smartContractAddressService = smartContractAddressService;
            Logger = NullLogger<BlockTransactionLimitChangedLogEventProcessor>.Instance;
        }

        public async Task ProcessAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
        {
            var eventData = new BlockTransactionLimitChanged();
            eventData.MergeFrom(logEvent);

            _blockTransactionLimitProvider.SetLimit(eventData.New,
                new BlockIndex {BlockHash = block.GetHash(), BlockHeight = block.Height});
            Logger.LogInformation($"BlockTransactionLimit has been changed to {eventData.New}");
            await Task.CompletedTask;
        }
    }
}