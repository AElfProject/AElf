using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.ResourceTokenFeeProvider.Impl;
using AElf.Kernel.SmartContract.Sdk;

namespace AElf.Kernel.FeeCalculation.Impl
{
    public class TxFeeProvider: TokenFeeProviderBase, IPrimaryTokenFeeProvider
    {
        public TxFeeProvider(ICoefficientsCacheProvider coefficientsCacheProvider) : base(
            coefficientsCacheProvider, (int)FeeTypeEnum.Tx)
        {
        }
        
        protected override void InitializeFunction()
        {
            PieceCalculateFunction = new PieceCalculateFunction();
            PieceCalculateFunction.AddFunction(LinerFunction).AddFunction(PowerFunction);
        }
        
        protected override int GetCalculateCount(ITransactionContext transactionContext)
        {
            return transactionContext.Transaction.Size();
        }
    }
}