using AElf.Contracts.MultiToken;
using AElf.Kernel.SmartContract;

namespace AElf.Kernel.FeeCalculation.Infrastructure
{
    internal class ReadFeeProvider : TokenFeeProviderBase, IResourceTokenFeeProvider
    {
        public ReadFeeProvider(ICalculateFunctionProvider calculateFunctionProvider) : base(
            calculateFunctionProvider, (int) FeeTypeEnum.Read)
        {

        }

        public string TokenName => "READ";

        protected override int GetCalculateCount(ITransactionContext transactionContext)
        {
            return transactionContext.Trace.StateSet.Reads.Count;
        }
    }
}