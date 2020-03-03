using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.ResourceTokenFeeProvider.Impl;
using AElf.Kernel.SmartContract.Sdk;
using Volo.Abp.DependencyInjection;

namespace AElf.Kernel.FeeCalculation.Impl
{
    public class StorageFeeProvider: TokenFeeProviderBase, IResourceTokenFeeProvider,ITransientDependency
    {
        public StorageFeeProvider(ICoefficientsCacheProvider coefficientsCacheProvider) : base(
            coefficientsCacheProvider, (int)FeeTypeEnum.Storage)
        {
        }
        
        public string TokenName { get; } = "STORAGE";
        
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