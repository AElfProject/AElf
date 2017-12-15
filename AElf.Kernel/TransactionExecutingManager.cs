using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics.Contracts;

namespace AElf.Kernel
{
    public class TransactionExecutingManager : ITransactionExecutingManager
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
                // group transactions by resource type
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
            //  execution strategy(experimental)
            //  1. tranform the dependency of Resource(R) into graph of related Transactions(T)
            //  2. find the T(ransaction) which owns the most neightbours
            //  3. exeucte the T(ransaction)
            //  4. check to see if this T(ransaction) leads to graph split
            //  5. if YES, we can parallel execute the transactions from the splitted graph
            //  6  if NO, goto step 2

            Task task = new Task(() =>
            {
        
            });

            // reset pending 
            pending = new Dictionary<IHash, List<ITransaction>>();
            return task;
        }
    }
}
