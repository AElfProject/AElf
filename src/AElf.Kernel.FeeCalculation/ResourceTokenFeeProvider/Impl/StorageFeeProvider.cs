using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.ResourceTokenFeeProvider.Impl;

namespace AElf.Kernel.FeeCalculation.Impl
{
    public class StorageFeeProvider: TokenFeeProviderBase, IResourceTokenFeeProvider
    {
        public StorageFeeProvider(ICoefficientsCacheProvider coefficientsCacheProvider) : base(
            coefficientsCacheProvider, (int)FeeTypeEnum.Storage)
        {
        }
        protected override void InitializeFunction()
        {
            PieceCalculateFunction = new PieceCalculateFunction();
            PieceCalculateFunction.AddFunction(LinerFunction).AddFunction(PowerFunction);
        }
    }
}