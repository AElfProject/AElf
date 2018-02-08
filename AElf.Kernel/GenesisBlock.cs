namespace AElf.Kernel
{
    public class GenesisBlock : IBlock
    {
        public IHash GetHash()
        {
            throw new System.NotImplementedException();
        }

        public IBlockHeader GetHeader()
        {
            throw new System.NotImplementedException();
        }

        public IBlockBody GetBody()
        {
            throw new System.NotImplementedException();
        }

        public bool AddTransaction(ITransaction tx)
        {
            throw new System.NotImplementedException();
        }
    }
}