using System.Collections.Generic;
using System.Threading;

namespace AElf.Kernel
{
    public class TransactionParallelMetaData:ITransactionParallelMetaData
    {
        public bool IsParallel()
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<IHash> GetDataConflict()
        {
            IHash<IAccount> a = null;
            IHash<IAccount> b = null;

            yield return a;
            yield return b;
            
        }
    }
}