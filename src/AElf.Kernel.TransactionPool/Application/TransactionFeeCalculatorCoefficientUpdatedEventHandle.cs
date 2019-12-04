using System;
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
    public class TransactionFeeCalculatorCoefficientUpdatedEventHandle : IBlockAcceptedLogEventHandler
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ICalculateFeeService _calculateFeeService;

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
            ICalculateFeeService calculateFeeService)
        {
            _smartContractAddressService = smartContractAddressService;
            _calculateFeeService = calculateFeeService;

            Logger = NullLogger<TransactionFeeCalculatorCoefficientUpdatedEventHandle>.Instance;
        }

        public async Task HandleAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
        {
            var eventData = new NoticeUpdateCalculateFeeAlgorithm();
            eventData.MergeFrom(logEvent);
            var blockIndex = new BlockIndex
            {
                BlockHash = block.GetHash(),
                BlockHeight = block.Height
            };
            var chainContext = new ChainContext
            {
                BlockHash = eventData.PreBlockHash,
                BlockHeight = eventData.BlockHeigh
            };
            var param = eventData.Parameter;
            var feeType = param.FeeType;
            var pieceKey = param.PieceKey;
            var funcType = param.FunctionType;
            var paramDic = param.ParameterDic;
            var opCode = param.OperationType;
            switch ((AlgorithmOpCode) opCode)
            {
                case AlgorithmOpCode.AddFunc:
                    await _calculateFeeService.AddFeeCal(chainContext, blockIndex, (FeeType) feeType, pieceKey,
                        (CalculateFunctionType) funcType, paramDic);
                    break;
                case AlgorithmOpCode.DeleteFunc:
                    await _calculateFeeService.DeleteFeeCal(chainContext, blockIndex, (FeeType) feeType, pieceKey);
                    break;
                case AlgorithmOpCode.UpdateFunc:
                    await _calculateFeeService.UpdateFeeCal(chainContext, blockIndex, (FeeType) feeType, pieceKey,
                        (CalculateFunctionType) funcType, paramDic);
                    break;
                default:
                    Logger.LogWarning($"does not find operation code {opCode}");
                    break;
            }
        }
    }
}