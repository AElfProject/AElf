using System.Threading.Tasks;
using Acs0;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.Token;
using AElf.Sdk.CSharp;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.TransactionPool.Application
{
    public class TransactionSizeFeeUnitPriceUpdatedEventHandler : IBlockAcceptedLogEventHandler
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITransactionSizeFeeUnitPriceProvider _transactionSizeFeeUnitPriceProvider;

        private LogEvent _interestedEvent;

        public ILogger<TransactionSizeFeeUnitPriceUpdatedEventHandler> Logger { get; set; }

        public LogEvent InterestedEvent
        {
            get
            {
                if (_interestedEvent != null)
                    return _interestedEvent;

                var address =
                    _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name);

                _interestedEvent = new TransactionSizeFeeUnitPriceUpdated().ToLogEvent(address);

                return _interestedEvent;
            }
        }

        public TransactionSizeFeeUnitPriceUpdatedEventHandler(ISmartContractAddressService smartContractAddressService,
            ITransactionSizeFeeUnitPriceProvider transactionSizeFeeUnitPriceProvider)
        {
            _smartContractAddressService = smartContractAddressService;
            _transactionSizeFeeUnitPriceProvider = transactionSizeFeeUnitPriceProvider;

            Logger = NullLogger<TransactionSizeFeeUnitPriceUpdatedEventHandler>.Instance;
        }

        public Task HandleAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
        {
            var eventData = new TransactionSizeFeeUnitPriceUpdated();
            eventData.MergeFrom(logEvent);

            _transactionSizeFeeUnitPriceProvider.SetUnitPrice(eventData.UnitPrice, new BlockIndex
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height
            });

            return Task.CompletedTask;
        }
    }
}