namespace AElf.Kernel.FeeCalculation.Impl
{
    public class TrafficFeeProvider : TokenFeeProviderBase, IResourceTokenFeeProvider
    {
        public const string Name = "TRAFFIC";

        public TrafficFeeProvider(ICoefficientsCacheProvider coefficientsCacheProvider) : base(
            coefficientsCacheProvider)
        {
        }

        protected override void InitializeFunction()
        {
            PieceCalculateFunction = new PieceCalculateFunction();
            PieceCalculateFunction.AddFunction(LinerFunction).AddFunction(PowerFunction);
        }

        protected override string GetTokenName()
        {
            return TokenName;
        }

        public string TokenName => Name;
    }
}