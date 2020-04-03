using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.SmartContract.ExecutionPluginForMethodFee
{
    public class SymbolListToPayTxFeeUpdatedLogEventProcessor : LogEventProcessorBase, IBlockAcceptedLogEventProcessor
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ITransactionSizeFeeSymbolsProvider _transactionSizeFeeSymbolsProvider;
        private LogEvent _interestedEvent;
        private ILogger<SymbolListToPayTxFeeUpdatedLogEventProcessor> Logger { get; set; }

        public override LogEvent InterestedEvent
        {
            get
            {
                if (_interestedEvent != null)
                    return _interestedEvent;

                var address =
                    _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name);

                _interestedEvent = new ExtraTokenListModified().ToLogEvent(address);

                return _interestedEvent;
            }
        }

        public SymbolListToPayTxFeeUpdatedLogEventProcessor(ISmartContractAddressService smartContractAddressService,
            ITransactionSizeFeeSymbolsProvider transactionSizeFeeSymbolsProvider)
        {
            _smartContractAddressService = smartContractAddressService;
            _transactionSizeFeeSymbolsProvider = transactionSizeFeeSymbolsProvider;
            Logger = NullLogger<SymbolListToPayTxFeeUpdatedLogEventProcessor>.Instance;
        }

        protected override async Task ProcessLogEventAsync(Block block, LogEvent logEvent)
        {
            var eventData = new ExtraTokenListModified();
            eventData.MergeFrom(logEvent);
            if (eventData.SymbolListToPayTxSizeFee == null)
                return;

            var transactionSizeFeeSymbols = new TransactionSizeFeeSymbols();
            foreach (var symbolToPayTxSizeFee in eventData.SymbolListToPayTxSizeFee.SymbolsToPayTxSizeFee)
            {
                transactionSizeFeeSymbols.TransactionSizeFeeSymbolList.Add(new TransactionSizeFeeSymbol
                {
                    TokenSymbol = symbolToPayTxSizeFee.TokenSymbol,
                    AddedTokenWeight = symbolToPayTxSizeFee.AddedTokenWeight,
                    BaseTokenWeight = symbolToPayTxSizeFee.BaseTokenWeight
                });
            }

            await _transactionSizeFeeSymbolsProvider.SetTransactionSizeFeeSymbolsAsync(new BlockIndex
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height
            }, transactionSizeFeeSymbols);
        }
    }
}