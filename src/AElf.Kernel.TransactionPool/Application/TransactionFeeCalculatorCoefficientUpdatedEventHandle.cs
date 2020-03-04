using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.SmartContractExecution.Application;
using AElf.Kernel.Token;
using AElf.Sdk.CSharp;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.TransactionPool.Application
{
    public class TransactionFeeCalculatorCoefficientUpdatedEventHandle : IBlockAcceptedLogEventHandler
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly IBlockchainStateService _blockChainStateService;
        private readonly ICoefficientsCacheProvider _coefficientsCacheProvider;


        private LogEvent _interestedEvent;

        private ILogger<TransactionFeeCalculatorCoefficientUpdatedEventHandle> Logger { get; set; }

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

        public TransactionFeeCalculatorCoefficientUpdatedEventHandle(
            ISmartContractAddressService smartContractAddressService,
            IBlockchainStateService blockChainStateService,
            ICoefficientsCacheProvider coefficientsCacheProvider)
        {
            _smartContractAddressService = smartContractAddressService;
            _blockChainStateService = blockChainStateService;
            _coefficientsCacheProvider = coefficientsCacheProvider;
            Logger = NullLogger<TransactionFeeCalculatorCoefficientUpdatedEventHandle>.Instance;
        }

        public async Task HandleAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
        {
            var eventData = new NoticeUpdateCalculateFeeAlgorithm();
            eventData.MergeFrom(logEvent);
            if (eventData.FeeType == (int) FeeTypeEnum.Tx)
            {
                var newCoefficient = new CalculateFeeCoefficientOfSender
                {
                    CoefficientOfSender = eventData.AllCoefficient
                };
                await _blockChainStateService.AddBlockExecutedDataAsync(block.GetHash(), newCoefficient);
            }
            else
            {
                var chainContext = new ChainContext
                {
                    BlockHash = block.GetHash(),
                    BlockHeight = block.Height
                };
                var existedCoefficient =
                    await _blockChainStateService.GetBlockExecutedDataAsync<CalculateFeeCoefficientOfContract>(
                        chainContext);
                existedCoefficient.CoefficientDicOfContract[eventData.FeeType] = eventData.AllCoefficient;
                await _blockChainStateService.AddBlockExecutedDataAsync(block.GetHash(), existedCoefficient);
            }

            _coefficientsCacheProvider.SetCoefficientByTokenType(eventData.FeeType);
        }
    }
}