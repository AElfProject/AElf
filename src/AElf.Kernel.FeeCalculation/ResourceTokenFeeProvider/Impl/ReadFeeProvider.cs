using AElf.Contracts.MultiToken;
using AElf.Kernel.FeeCalculation.ResourceTokenFeeProvider.Impl;

namespace AElf.Kernel.FeeCalculation.Impl
{
    public class ReadFeeProvider: TokenFeeProviderBase, IResourceTokenFeeProvider
    {
        public ReadFeeProvider(ICoefficientsCacheProvider coefficientsCacheProvider) : base(
            coefficientsCacheProvider, (int)FeeTypeEnum.Read)
        {
        }
        protected override void InitializeFunction()
        {
            PieceCalculateFunction = new PieceCalculateFunction();
            PieceCalculateFunction.AddFunction(LinerFunction).AddFunction(LinerFunction).AddFunction(PowerFunction);
        }
    }
}