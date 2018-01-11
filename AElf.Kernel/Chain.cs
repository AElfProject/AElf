namespace AElf.Kernel
{
    public class Chain : IChain
    {
        public long CurrentBlockHeight => throw new System.NotImplementedException();

        public IHash<IBlock> CurrentBlockHash => throw new System.NotImplementedException();
    }
}