using System;
using System.Linq;
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
        private readonly ICalculateTxCostStrategy _txCostStrategy;
        private readonly ICalculateCpuCostStrategy _cpuCostStrategy;
        private readonly ICalculateRamCostStrategy _ramCostStrategy;
        private readonly ICalculateNetCostStrategy _netCostStrategy;
        private readonly ICalculateStoCostStrategy _stoCostStrategy;

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
            ICalculateTxCostStrategy txCostStrategy,
            ICalculateCpuCostStrategy cpuCostStrategy,
            ICalculateRamCostStrategy ramCostStrategy,
            ICalculateStoCostStrategy stoCostStrategy,
            ICalculateNetCostStrategy netCostStrategy)
        {
            _smartContractAddressService = smartContractAddressService;
            _txCostStrategy = txCostStrategy;
            _cpuCostStrategy = cpuCostStrategy;
            _ramCostStrategy = ramCostStrategy;
            _stoCostStrategy = stoCostStrategy;
            _netCostStrategy = netCostStrategy;
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
            foreach (var coefficient in eventData.AllCoefficient.Coefficients)
            {
                var selectedStrategy = coefficient.FeeType switch
                {
                    FeeTypeEnum.Tx => (ICalculateCostStrategy) _txCostStrategy,
                    FeeTypeEnum.Cpu => _cpuCostStrategy,
                    FeeTypeEnum.Ram => _ramCostStrategy,
                    FeeTypeEnum.Sto => _stoCostStrategy,
                    FeeTypeEnum.Net => _netCostStrategy,
                    _ => throw new ArgumentOutOfRangeException()
                };
                if (coefficient.PieceKey == eventData.OldPieceKey)
                {
                    await selectedStrategy.ChangeAlgorithmPieceKeyAsync(blockIndex, 
                        eventData.OldPieceKey, eventData.NewPieceKey);
                }
                else
                {
                    var pieceKey = coefficient.PieceKey;
                    var paramDic = coefficient.CoefficientDic.ToDictionary(x => x.Key.ToLower(), x => x.Value);
                    await selectedStrategy.ModifyAlgorithmAsync(blockIndex, pieceKey, paramDic);
                }
                
            }
        }
    }
}