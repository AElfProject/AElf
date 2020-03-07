using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    internal class TxFeeProvider : TokenFeeProviderBase, IPrimaryTokenFeeProvider, ITransientDependency
    {
        public TxFeeProvider(ICoefficientsCacheProvider coefficientsCacheProvider,
            ICalculateFunctionFactory calculateFunctionFactory) : base(
            coefficientsCacheProvider, calculateFunctionFactory, (int) FeeTypeEnum.Tx)
        {

        }

        protected override int GetCalculateCount(ITransactionContext transactionContext)
        {
            return transactionContext.Transaction.Size();
        }
    }
}