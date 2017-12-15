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
        private Dictionary<IHash, List<ITransaction>> pending = new Dictionary<IHash, List<ITransaction>>();

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
                // step 1: group transaction by resource types
                var conflicts = tx.GetParallelMetaData().GetDataConflict();
                this.mut.WaitOne();
                foreach (var res in conflicts)
                {
                    if (pending[res] != null) {
                        pending[res] = new List<ITransaction>();

                    }
                    pending[res].Add(tx);

                }
                this.mut.ReleaseMutex();
            });
            task.Start();
                 
            await task;
        }

        /// <summary>
        /// Schedule execution of transaction
        /// </summary>
        Task Scheduler() {
            // TODO: step 2: generate a DAG for dependency


            // TODO: step 3: execution on the DAG
            Task task = new Task(() =>
            {
        
            });

            // TODO: step 4: reset pending 
            pending = new Dictionary<IHash, List<ITransaction>>();
            return task;
        }
    }
}
