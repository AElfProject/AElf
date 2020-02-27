using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using AElf.Contracts.Configuration;
using AElf.Kernel.SmartContractExecution;
using AElf.Kernel.SmartContractExecution.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.BlockTransactionLimitController
{
    public class BlockTransactionLimitChangedLogEventHandler : IBlockAcceptedLogEventHandler
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

                _interestedEvent = new BlockTransactionLimitChanged().ToLogEvent(address);

                return _interestedEvent;
            }
        }

        public ILogger<BlockTransactionLimitChangedLogEventHandler> Logger { get; set; }

        public BlockTransactionLimitChangedLogEventHandler(
            ISmartContractAddressService smartContractAddressService, IBlockchainStateService blockchainStateService)
        {
            _smartContractAddressService = smartContractAddressService;
            _blockchainStateService = blockchainStateService;
            Logger = NullLogger<BlockTransactionLimitChangedLogEventHandler>.Instance;
        }

        public async Task HandleAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
        {
            var eventData = new BlockTransactionLimitChanged();
            eventData.MergeFrom(logEvent);

            var limit = new BlockTransactionLimit
            {
                Value = eventData.New
            };
            await _blockchainStateService.AddBlockExecutedDataAsync(block.GetHash(), limit);

            Logger.LogInformation($"BlockTransactionLimit has been changed to {eventData.New}");
        }
    }
}