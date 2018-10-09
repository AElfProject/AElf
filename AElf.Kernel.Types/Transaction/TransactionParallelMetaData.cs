using System.Collections.Generic;
using AElf.Common;

namespace AElf.Kernel
{
    public class TransactionParallelMetaData: ITransactionParallelMetaData
    {
        public bool IsParallel()
        {
            throw new System.NotImplementedException();
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