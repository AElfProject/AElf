using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using AElf.Contracts.Configuration;
using AElf.CSharp.Core.Extension;
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
        
        public BlockTransactionLimitChangedLogEventProcessor(ISmartContractAddressService smartContractAddressService,
            IBlockTransactionLimitProvider blockTransactionLimitProvider)
        {
            _smartContractAddressService = smartContractAddressService;
            _blockTransactionLimitProvider = blockTransactionLimitProvider;
            Logger = NullLogger<BlockTransactionLimitChangedLogEventProcessor>.Instance;
        }

        public async Task ProcessAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
        {
            var eventData = new BlockTransactionLimitChanged();
            eventData.MergeFrom(logEvent);
            await _blockTransactionLimitProvider.SetLimitAsync(block.GetHash(), eventData.New);
            Logger.LogInformation($"BlockTransactionLimit has been changed to {eventData.New}");
        }
    }
}