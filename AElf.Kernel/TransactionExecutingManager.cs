using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    public class TransactionExecutingManager:ITransactionExecutingManager
    {
        Dictionary<IHash, IEnumerable<ITransaction>> pending;
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
                foreach (var queue in pending)
                {

                }
            });
            task.Start();
            await task;
        }
    }
}
