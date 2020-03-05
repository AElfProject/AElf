using AElf.Contracts.MultiToken;
using Volo.Abp.DependencyInjection;
using AElf.Kernel.SmartContract;

namespace AElf.Kernel.FeeCalculation
{
    public class StorageFeeProvider : TokenFeeProviderBase, IResourceTokenFeeProvider, ITransientDependency
    {
        private readonly ICalculateFunctionProvider _calculateFunctionProvider;

        public StorageFeeProvider(ICoefficientsCacheProvider coefficientsCacheProvider,
            ICalculateFunctionProvider calculateFunctionProvider) : base(
            coefficientsCacheProvider, (int) FeeTypeEnum.Storage)
        {
            _calculateFunctionProvider = calculateFunctionProvider;
        }

        public string TokenName { get; } = "STORAGE";

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