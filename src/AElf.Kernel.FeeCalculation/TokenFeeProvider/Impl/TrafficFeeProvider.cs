using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.ResourceTokenFeeProvider.Impl;
using AElf.Kernel.SmartContract.Sdk;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeeCalculation.Impl
{
    public class TrafficFeeProvider : TokenFeeProviderBase, IResourceTokenFeeProvider, ITransientDependency
    {
        private readonly ICalculateFunctionProvider _calculateFunctionProvider;

        public TrafficFeeProvider(ICoefficientsCacheProvider coefficientsCacheProvider,
            ICalculateFunctionProvider calculateFunctionProvider) : base(
            coefficientsCacheProvider, (int) FeeTypeEnum.Traffic)
        {
            _calculateFunctionProvider = calculateFunctionProvider;
        }

        public string TokenName { get; } = "TRAFFIC";

        protected override void InitializeFunction()
        {
            PieceCalculateFunction = new PieceCalculateFunction();
            PieceCalculateFunction.AddFunction(_calculateFunctionProvider.LinerFunction)
                .AddFunction(_calculateFunctionProvider.PowerFunction);
        }

        protected override int GetCalculateCount(ITransactionContext transactionContext)
        {
            return transactionContext.Transaction.Size();
        }
    }
}