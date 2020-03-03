using AElf.Kernel.FeeCalculation.ResourceTokenFeeProvider.Impl;
using AElf.Kernel.SmartContract.Sdk;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeeCalculation.Impl
{
    public class WriteFeeProvider: TokenFeeProviderBase, IResourceTokenFeeProvider,ITransientDependency
    {
        public WriteFeeProvider(ICoefficientsCacheProvider coefficientsCacheProvider) : base(
            coefficientsCacheProvider, 2)
        {
        }
        
        public string TokenName { get; } = "WRITE";
        
        protected override void InitializeFunction()
        {
            PieceCalculateFunction = new PieceCalculateFunction();
            PieceCalculateFunction.AddFunction(LinerFunction).AddFunction(LinerFunction).AddFunction(PowerFunction);
        }
        protected override int GetCalculateCount(ITransactionContext transactionContext)
        {
            return transactionContext.Trace.StateSet.Writes.Count;
        }
    }
}