namespace AElf.Kernel
{
    public class Path
    {
        public bool IsPointer { get; set; } = false;

        private IHash<IChain> _chainHash = new Hash<IChain>();
        private IHash<IBlock> _blockHash;
        private IHash<IAccount> _accountAddress = new Hash<IAccount>();

        public Path()
        {
            
        }

        public Path SetChainHash(IHash<IChain> chainHash)
        {
            _chainHash = chainHash;
            return this;
        }
        
        public Path SetBlockHash(IHash<IBlock> blockHash)
        {
            _blockHash = blockHash;
            IsPointer = true;
            return this;
        }
        
        public Path SetAccount(IHash<IAccount> accountAddress)
        {
            _accountAddress = accountAddress;
            return this;
        }
    }
}