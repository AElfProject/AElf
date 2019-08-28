using System.Threading.Tasks;
using AElf.Kernel.Miner.Application;
using AElf.Kernel.SmartContract.Application;
using AElf.Sdk.CSharp;
using AElf.Types;
using AElf.Contracts.Configuration;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.BlockTransactionLimitController
{
    public class BlockTransactionLimitChangedLogEventHandler : ILogEventHandler, ISingletonDependency
    {
        private readonly IBlockTransactionLimitProvider _provider;
        private readonly ISmartContractAddressService _smartContractAddressService;
        private LogEvent _interestedEvent;

        public LogEvent InterestedEvent
        {
            get
            {
                if (_interestedEvent != null) return _interestedEvent;
                var address =
                    _smartContractAddressService.GetAddressByContractName(ConfigurationSmartContractAddressNameProvider.Name);

                _interestedEvent = new BlockTransactionLimitChanged().ToLogEvent(address);

                return _interestedEvent;
            }
        }

        public ILogger<BlockTransactionLimitChangedLogEventHandler> Logger { get; set; }

        public BlockTransactionLimitChangedLogEventHandler(IBlockTransactionLimitProvider provider,
            ISmartContractAddressService smartContractAddressService)
        {
            _provider = provider;
            _smartContractAddressService = smartContractAddressService;
            Logger = NullLogger<BlockTransactionLimitChangedLogEventHandler>.Instance;
        }

        public async Task Handle(Block block, TransactionResult result, LogEvent log)
        {
            var eventData = new BlockTransactionLimitChanged();
            foreach (var bs in log.Indexed)
            {
                eventData.MergeFrom(bs);
            }

            eventData.MergeFrom(log.NonIndexed);
            _provider.SetLimit(eventData.New);
            Logger.LogInformation($"BlockTransactionLimit has been changed to {eventData.New}");
            await Task.CompletedTask;
        }
    }
}