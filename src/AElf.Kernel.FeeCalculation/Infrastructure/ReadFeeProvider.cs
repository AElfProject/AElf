using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    internal class ReadFeeProvider : TokenFeeProviderBase, IResourceTokenFeeProvider, ITransientDependency
    {
        public ReadFeeProvider(ICoefficientsCacheProvider coefficientsCacheProvider,
            ICalculateFunctionFactory calculateFunctionFactory) : base(
            coefficientsCacheProvider, calculateFunctionFactory, (int) FeeTypeEnum.Read)
        {

        }

        public string TokenName => "READ";

        protected override int GetCalculateCount(ITransactionContext transactionContext)
        {
            return transactionContext.Trace.StateSet.Reads.Count;
        }
    }
}