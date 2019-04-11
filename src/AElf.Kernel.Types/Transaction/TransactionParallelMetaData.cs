using System;
using System.Collections.Generic;
using AElf.Types;

namespace AElf.Kernel
{
    public class TransactionParallelMetaData : ITransactionParallelMetaData
    {
        public bool IsParallel()
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Hash> GetDataConflict()
        {
            Hash a = null;
            Hash b = null;

            yield return a;
            yield return b;
        }
    }
}