using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

namespace AElf.Kernel
{
    public class TransactionExecutingManager:ITransactionExecutingManager
    {
        private Mutex mut = new Mutex();
        private Dictionary<IHash, IEnumerable<ITransaction>> pending;

        public TransactionExecutingManager()
        {
            pending=new Dictionary<IHash, IEnumerable<ITransaction>>();
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

                this.mut.ReleaseMutex();
            });
            task.Start();
                 
            await task;
        }

        /// <summary>
        /// Schedule execution of transaction
        /// </summary>
        void Scheduler() {
            foreach (var queue in pending)
            {
                foreach (var a in queue.Value)
                {

                }
            }
        }
    }
}
