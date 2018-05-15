using System.Collections;
using System.Collections.Generic;

namespace AElf.Kernel.Concurrency
{
    public class Job : IEnumerable<ITransaction>
    {
        public readonly List<ITransaction> TxList = new List<ITransaction>();

        public void AddTx(ITransaction tx)
        {
            TxList.Add(tx);
        }

        public void MergeJob(Job job)
        {
            TxList.AddRange(job.TxList);
        }

        public IEnumerator<ITransaction> GetEnumerator()
        {
            return TxList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}