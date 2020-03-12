using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    internal class StorageFeeProvider : TokenFeeProviderBase, IResourceTokenFeeProvider
    {
        public StorageFeeProvider(ICalculateFunctionProvider calculateFunctionProvider) : base(
            calculateFunctionProvider, (int) FeeTypeEnum.Storage)
        {

        }

        public string TokenName => "STORAGE";

        protected override int GetCalculateCount(ITransactionContext transactionContext)
        {
            return transactionContext.Transaction.Size();
        }
    }
}