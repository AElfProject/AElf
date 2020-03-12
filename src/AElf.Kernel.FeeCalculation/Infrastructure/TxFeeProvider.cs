using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    internal class TxFeeProvider : TokenFeeProviderBase, IPrimaryTokenFeeProvider
    {
        public TxFeeProvider(ICoefficientsProvider coefficientsProvider,
            ICalculateFunctionProvider calculateFunctionProvider) : base(
            coefficientsProvider, calculateFunctionProvider, (int) FeeTypeEnum.Tx)
        {

        }

        protected override int GetCalculateCount(ITransactionContext transactionContext)
        {
            return transactionContext.Transaction.Size();
        }
    }
}