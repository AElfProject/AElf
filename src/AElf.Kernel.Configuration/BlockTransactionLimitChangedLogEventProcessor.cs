using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel.SmartContract.Application;
using AElf.Types;
using AElf.Contracts.Configuration;
using AElf.CSharp.Core.Extension;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.Configuration
{
    public class BlockTransactionLimitChangedLogEventProcessor : LogEventProcessorBase, IBlockAcceptedLogEventProcessor
    {
        private readonly IBlockTransactionLimitProvider _blockTransactionLimitProvider;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private LogEvent _interestedEvent;

        public override LogEvent InterestedEvent
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
            IBlockTransactionLimitProvider blockTransactionLimitProvider)
        {
            _smartContractAddressService = smartContractAddressService;
            _blockTransactionLimitProvider = blockTransactionLimitProvider;
            Logger = NullLogger<BlockTransactionLimitChangedLogEventProcessor>.Instance;
        }

        protected override async Task ProcessLogEventAsync(Block block, LogEvent logEvent)
        {
            var configurationSet = new ConfigurationSet();
            configurationSet.MergeFrom(logEvent);

            if (configurationSet.Key != BlockTransactionLimitConfigurationNameProvider.Name) return;

            var limit = new Int32Value();
            limit.MergeFrom(configurationSet.Value.ToByteArray());
            if (limit.Value < 0) return;
            await _blockTransactionLimitProvider.SetLimitAsync(new BlockIndex
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height
            }, limit.Value);

            Logger.LogInformation($"BlockTransactionLimit has been changed to {limit.Value}");
        }
    }
}