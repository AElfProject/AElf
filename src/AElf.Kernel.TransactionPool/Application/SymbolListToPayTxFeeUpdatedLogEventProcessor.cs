using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContract.ExecutionPluginForMethodFee;
using AElf.Kernel.Token;
using AElf.Sdk.CSharp;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;


namespace AElf.Kernel.TransactionPool.Application
{
    public class SymbolListToPayTxFeeUpdatedLogEventProcessor : IBlockAcceptedLogEventProcessor
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IBlockchainStateService _blockChainStateService;
        private LogEvent _interestedEvent;
        private ILogger<SymbolListToPayTxFeeUpdatedLogEventProcessor> Logger { get; set; }

        public LogEvent InterestedEvent
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
            IBlockchainStateService blockChainStateService)
        {
            _smartContractAddressService = smartContractAddressService;
            _blockChainStateService = blockChainStateService;
            Logger = NullLogger<SymbolListToPayTxFeeUpdatedLogEventProcessor>.Instance;
        }

        public async Task ProcessAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
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

            await _blockChainStateService.AddBlockExecutedDataAsync(block.GetHash(), transactionSizeFeeSymbols);
        }
    }
}