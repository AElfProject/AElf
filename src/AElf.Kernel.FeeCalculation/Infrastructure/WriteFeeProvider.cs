using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    internal class WriteFeeProvider : TokenFeeProviderBase, IResourceTokenFeeProvider
    {
        public WriteFeeProvider(ICalculateFunctionProvider calculateFunctionProvider) : base(
            calculateFunctionProvider, (int) FeeTypeEnum.Write)
        {

        }

        public string TokenName => "WRITE";

        protected override int GetCalculateCount(ITransactionContext transactionContext)
        {
            return transactionContext.Trace.StateSet.Writes.Count;
        }
    }
}