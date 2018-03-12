namespace AElf.Kernel
{
    public class Path
    {
        public bool IsPointer = false;
        
        private IHash<IChain> _chainHash = new Hash<IChain>();
        private IHash<IBlock> _blockHash;
        private IHash<IAccount> _accountAddress = new Hash<IAccount>();

        public Path()
        {
            
        }
    }
}