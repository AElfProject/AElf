namespace AElf.ChainController.EventMessages
{
    public class LockMining
    {
        public bool Lock { get; }

        public LockMining(bool @lock)
        {
            Lock = @lock;
        }
    }
}