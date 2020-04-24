using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    internal class TrafficFeeProvider : TokenFeeProviderBase, IResourceTokenFeeProvider
    {
        public TrafficFeeProvider(ICalculateFunctionProvider calculateFunctionProvider) : base(
            calculateFunctionProvider, (int) FeeTypeEnum.Traffic)
        {

        }

        public string TokenName => "TRAFFIC";

        protected override int GetCalculateCount(ITransactionContext transactionContext)
        {
            return transactionContext.Transaction.Size();
        }
    }
}