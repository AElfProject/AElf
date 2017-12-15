using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics.Contracts;

namespace AElf.Kernel
{
    public class TransactionExecutingManager:ITransactionExecutingManager
    {
        private Mutex mut = new Mutex();
        private Dictionary<IHash, IEnumerable<ITransaction>> pending = new Dictionary<IHash, IEnumerable<ITransaction>>();

        public TransactionExecutingManager()
        {
        }

        /// <summary>
        /// AEs the lf. kernel. IT ransaction executing manager. execute async.
        /// </summary>
        /// <returns>The lf. kernel. IT ransaction executing manager. execute async.</returns>
        /// <param name="tx">Tx.</param>
        async Task ITransactionExecutingManager.ExecuteAsync(ITransaction tx)
        {
            Task task = new Task(() =>
            {
                // TODO: seperate transactions into un-related groups
                this.mut.WaitOne();
                var md = tx.GetParallelMetaData();
                this.mut.ReleaseMutex();
            });
            task.Start();
                 
            await task;
        }

        /// <summary>
        /// Schedule execution of transaction
        /// </summary>
        Task Scheduler() {
            Task task = new Task(() =>
            {
                foreach (var queue in pending)
                {
                    foreach (var tx in queue.Value)
                    {
                    }
                }
            });
            return task;
        }
    }
}
