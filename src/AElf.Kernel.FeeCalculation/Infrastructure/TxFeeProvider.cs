using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    internal class TxFeeProvider : TokenFeeProviderBase, IPrimaryTokenFeeProvider
    {
        public TxFeeProvider(ICalculateFunctionProvider calculateFunctionProvider) : base(
            calculateFunctionProvider, (int) FeeTypeEnum.Tx)
        {

        }

        protected override int GetCalculateCount(ITransactionContext transactionContext)
        {
            return transactionContext.Transaction.Size();
        }
    }
}