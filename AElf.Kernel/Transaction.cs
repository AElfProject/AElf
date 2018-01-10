using System;
using System.Collections.Generic;
using System.Text;

namespace AElf.Kernel
{
    public class Transaction : ITransaction
    {
        public IHash<ITransaction> GetHash()
        {
            throw new NotImplementedException();
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
