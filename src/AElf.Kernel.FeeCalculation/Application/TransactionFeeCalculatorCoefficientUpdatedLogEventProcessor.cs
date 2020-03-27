using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
using AElf.Kernel.FeeCalculation.Extensions;
using AElf.Kernel.FeeCalculation.Infrastructure;
using AElf.Kernel.SmartContract.Application;
using AElf.Kernel.Token;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Kernel.FeeCalculation.Application
{
    public class TransactionFeeCalculatorCoefficientUpdatedLogEventProcessor : IBlockAcceptedLogEventProcessor
    {
        private readonly ISmartContractAddressService _smartContractAddressService;
        private readonly ICalculateFunctionProvider _calculateFunctionProvider;

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

                _interestedEvent = new CalculateFeeAlgorithmUpdated().ToLogEvent(address);

                return _interestedEvent;
            }
        }

        public TransactionFeeCalculatorCoefficientUpdatedLogEventProcessor(
            ISmartContractAddressService smartContractAddressService,
            ICalculateFunctionProvider calculateFunctionProvider)
        {
            _smartContractAddressService = smartContractAddressService;
            _calculateFunctionProvider = calculateFunctionProvider;
            Logger = NullLogger<TransactionFeeCalculatorCoefficientUpdatedLogEventProcessor>.Instance;
        }

        public async Task ProcessAsync(Block block, Dictionary<TransactionResult, List<LogEvent>> logEventsMap)
        {
            foreach (var logEvents in logEventsMap.Values)
            {
                foreach (var logEvent in logEvents)
                {
                    var eventData = new CalculateFeeAlgorithmUpdated();
                    eventData.MergeFrom(logEvent);
                    await _calculateFunctionProvider.AddCalculateFunctions(new BlockIndex
                    {
                        BlockHash = block.GetHash(),
                        BlockHeight = block.Height
                    }, eventData.AllTypeFeeCoefficients.ToCalculateFunctionDictionary());
                }
            }
        }
    }
}