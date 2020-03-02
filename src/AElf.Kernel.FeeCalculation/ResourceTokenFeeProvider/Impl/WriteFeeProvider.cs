using AElf.Kernel.FeeCalculation.ResourceTokenFeeProvider.Impl;

namespace AElf.Kernel.FeeCalculation.Impl
{
    public class WriteFeeProvider: TokenFeeProviderBase, IResourceTokenFeeProvider
    {
        public WriteFeeProvider(ICoefficientsCacheProvider coefficientsCacheProvider) : base(
            coefficientsCacheProvider, 2)
        {
        }
        protected override void InitializeFunction()
        {
            PieceCalculateFunction = new PieceCalculateFunction();
            PieceCalculateFunction.AddFunction(LinerFunction).AddFunction(LinerFunction).AddFunction(PowerFunction);
        }
    }
}