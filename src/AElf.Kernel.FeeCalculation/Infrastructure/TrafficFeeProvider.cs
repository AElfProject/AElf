using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    internal class TrafficFeeProvider : TokenFeeProviderBase, IResourceTokenFeeProvider, ITransientDependency
    {
        public TrafficFeeProvider(ICoefficientsProvider coefficientsProvider,
            ICalculateFunctionProvider calculateFunctionProvider) : base(
            coefficientsProvider, calculateFunctionProvider, (int) FeeTypeEnum.Traffic)
        {

        }

        public string TokenName => "TRAFFIC";

        protected override int GetCalculateCount(ITransactionContext transactionContext)
        {
            return transactionContext.Transaction.Size();
        }
    }
}