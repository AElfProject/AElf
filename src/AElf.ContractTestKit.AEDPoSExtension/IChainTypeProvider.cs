namespace AElf.ContractTestKit.AEDPoSExtension
{
    public interface IChainTypeProvider
    {
        bool IsSideChain { get; set; }
    }

    public class ChainTypeProvider : IChainTypeProvider
    {
        public bool IsSideChain { get; set; } = false;
    }
}