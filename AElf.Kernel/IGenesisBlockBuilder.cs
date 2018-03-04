namespace AElf.Kernel
{
    public interface IGenesisBlockBuilder
    {
        IGenesisBlock Build(IHash<IChain> chainId,);
    }

    public class GenesisBlockBuilder : IGenesisBlockBuilder
    {
        
    }
}