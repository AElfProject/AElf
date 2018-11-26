namespace AElf.ChainController.EventMessages
{
    public sealed class LockMining
    {
        public bool Lock { get; }

        public LockMining(bool @lock)
        {
            Lock = @lock;
        }
    }
}