using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.ResourceTokenFeeProvider.Impl;

namespace AElf.Kernel.FeeCalculation.Impl
{
    public class TrafficFeeProvider : TokenFeeProviderBase, IResourceTokenFeeProvider
    {
        public TrafficFeeProvider(ICoefficientsCacheProvider coefficientsCacheProvider) : base(
            coefficientsCacheProvider, (int)FeeTypeEnum.Traffic)
        {
        }

        protected override void InitializeFunction()
        {
            PieceCalculateFunction = new PieceCalculateFunction();
            PieceCalculateFunction.AddFunction(LinerFunction).AddFunction(PowerFunction);
        }
    }
}