using System.Collections.Generic;

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
            IHash a = null;
            IHash b = null;

            yield return a;
            yield return b;
            
        }
    }
}