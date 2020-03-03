using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.ResourceTokenFeeProvider.Impl;
using AElf.Kernel.SmartContract.Sdk;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeeCalculation.Impl
{
    public class ReadFeeProvider: TokenFeeProviderBase, IResourceTokenFeeProvider,ITransientDependency
    {
        public ReadFeeProvider(ICoefficientsCacheProvider coefficientsCacheProvider) : base(
            coefficientsCacheProvider, (int)FeeTypeEnum.Read)
        {
        }
        public string TokenName { get; } = "READ";

        protected override void InitializeFunction()
        {
            PieceCalculateFunction = new PieceCalculateFunction();
            PieceCalculateFunction.AddFunction(LinerFunction).AddFunction(LinerFunction).AddFunction(PowerFunction);
        }
        
        protected override int GetCalculateCount(ITransactionContext transactionContext)
        {
            return transactionContext.Trace.StateSet.Reads.Count;
        }
    }
}