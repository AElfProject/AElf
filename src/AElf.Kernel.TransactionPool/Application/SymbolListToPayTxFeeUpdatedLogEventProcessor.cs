using System.Collections.Generic;
using System.Threading.Tasks;
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
    //TODO: not here

    public class SymbolListToPayTxFeeUpdatedLogEventProcessor : IBlockAcceptedLogEventProcessor
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ISymbolListToPayTxFeeService _symbolListToPayTxFeeService;
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
            ISymbolListToPayTxFeeService symbolListToPayTxFeeService)
        {
            _smartContractAddressService = smartContractAddressService;
            _symbolListToPayTxFeeService = symbolListToPayTxFeeService;
            Logger = NullLogger<SymbolListToPayTxFeeUpdatedLogEventProcessor>.Instance;
        }

        public Task ProcessAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
        {
            var eventData = new ExtraTokenListModified();
            eventData.MergeFrom(logEvent);
            var blockIndex = new BlockIndex
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height
            };
            if (eventData.SymbolListToPayTxSizeFee == null)
                return Task.CompletedTask;
            var newTokenInfoList = new List<AvailableTokenInfoInCache>();
            foreach (var tokenInfo in eventData.SymbolListToPayTxSizeFee.SymbolsToPayTxSizeFee)
            {
                newTokenInfoList.Add(new AvailableTokenInfoInCache
                {
                    TokenSymbol = tokenInfo.TokenSymbol,
                    AddedTokenWeight = tokenInfo.AddedTokenWeight,
                    BaseTokenWeight = tokenInfo.BaseTokenWeight
                });
            }

            _symbolListToPayTxFeeService.SetExtraAcceptedTokenInfoToForkCache(blockIndex, newTokenInfoList);
            return Task.CompletedTask;
        }
    }
}