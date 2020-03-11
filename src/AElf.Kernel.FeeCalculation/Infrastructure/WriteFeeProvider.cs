using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    internal class WriteFeeProvider : TokenFeeProviderBase, IResourceTokenFeeProvider, ITransientDependency
    {
        public WriteFeeProvider(ICoefficientsProvider coefficientsProvider,
            ICalculateFunctionProvider calculateFunctionProvider) : base(
            coefficientsProvider, calculateFunctionProvider, (int) FeeTypeEnum.Write)
        {

        }

        public string TokenName => "WRITE";

        protected override int GetCalculateCount(ITransactionContext transactionContext)
        {
            return transactionContext.Trace.StateSet.Writes.Count;
        }
    }
}