using System;

namespace AElf.Kernel
{
    public class Transaction : ITransaction
    {
        public IAccount AccountTo { get; set; }

        public IAccount AccountFrom { get; set; }

        public int Amount { get; set; }

        public Transaction() { }

        public IHash<ITransaction> GetHash()
        {
            return new Hash<ITransaction>(ExtensionMethods.GetHash(this));
        }

        public ITransactionParallelMetaData GetParallelMetaData()
        {
            throw new NotImplementedException();
        }

        public IHash<IBlockHeader> LastBlockHashWhenCreating()
        {
            throw new NotImplementedException();
        }
    }
}
