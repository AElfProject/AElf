using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Contracts.MultiToken;
using AElf.CSharp.Core.Extension;
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
        private readonly ICoefficientsProvider _coefficientsProvider;
        private readonly IServiceContainer<IPrimaryTokenFeeProvider> _primaryTokenFeeProviders;
        private readonly IServiceContainer<IResourceTokenFeeProvider> _resourceTokenFeeProviders;

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
            ICoefficientsProvider coefficientsProvider,
            IServiceContainer<IPrimaryTokenFeeProvider> primaryTokenFeeProviders,
            IServiceContainer<IResourceTokenFeeProvider> resourceTokenFeeProviders)
        {
            _smartContractAddressService = smartContractAddressService;
            _coefficientsProvider = coefficientsProvider;
            _primaryTokenFeeProviders = primaryTokenFeeProviders;
            _resourceTokenFeeProviders = resourceTokenFeeProviders;
            Logger = NullLogger<TransactionFeeCalculatorCoefficientUpdatedLogEventProcessor>.Instance;
        }

        public async Task ProcessAsync(Block block, TransactionResult transactionResult, LogEvent logEvent)
        {
            var eventData = new CalculateFeeAlgorithmUpdated();
            eventData.MergeFrom(logEvent);
            await _coefficientsProvider.SetAllCoefficientsAsync(block.GetHash(), eventData.AllTypeFeeCoefficients);
            foreach (var feeProvider in _primaryTokenFeeProviders)
            {
                feeProvider.UpdatePieceWiseFunction(eventData.AllTypeFeeCoefficients.Value
                    .Single(x => x.FeeTokenType == (int) FeeTypeEnum.Tx).PieceCoefficientsList.AsEnumerable()
                    .Select(x => x.Value.ToArray()).ToList());
            }

            foreach (var feeProvider in _resourceTokenFeeProviders)
            {
                feeProvider.UpdatePieceWiseFunction(eventData.AllTypeFeeCoefficients.Value
                    .Single(x => string.Equals(((FeeTypeEnum) x.FeeTokenType).ToString(), feeProvider.TokenName,
                        StringComparison.CurrentCultureIgnoreCase))
                    .PieceCoefficientsList.AsEnumerable()
                    .Select(x => x.Value.ToArray()).ToList());
            }
        }
    }
}