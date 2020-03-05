using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Sdk.CSharp;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.FeeCalculation.Application
{
    public class TransactionFeeCalculatorCoefficientUpdatedLogEventProcessor : IBlockAcceptedLogEventProcessor
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IBlockchainStateService _blockChainStateService;
        private readonly ICoefficientsCacheProvider _coefficientsCacheProvider;


        private LogEvent _interestedEvent;

        private ILogger<TransactionFeeCalculatorCoefficientUpdatedLogEventProcessor> Logger { get; set; }

        public LogEvent InterestedEvent
        {
            get
            {
                if (_interestedEvent != null)
                    return _interestedEvent;

                var address =
                    _smartContractAddressService.GetAddressByContractName(TokenSmartContractAddressNameProvider.Name);

                _interestedEvent = new NoticeUpdateCalculateFeeAlgorithm().ToLogEvent(address);

                return _interestedEvent;
            }
        }

        public TransactionFeeCalculatorCoefficientUpdatedLogEventProcessor(
            ISmartContractAddressService smartContractAddressService,
            IBlockchainStateService blockChainStateService,
            ICoefficientsCacheProvider coefficientsCacheProvider)
        {
            _smartContractAddressService = smartContractAddressService;
            _blockChainStateService = blockChainStateService;
            _coefficientsCacheProvider = coefficientsCacheProvider;
            Logger = NullLogger<TransactionFeeCalculatorCoefficientUpdatedLogEventProcessor>.Instance;
        }

        public async Task ProcessAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
        {
            var eventData = new NoticeUpdateCalculateFeeAlgorithm();
            eventData.MergeFrom(logEvent);
            await _blockChainStateService.AddBlockExecutedDataAsync(block.GetHash(), eventData.CoefficientOfAllType);
            if(block.Height > 1)
                _coefficientsCacheProvider.SetModifyHeight(block.Height);
        }
    }
}