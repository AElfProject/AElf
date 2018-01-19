using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    public class Worker : IWorker
    {
        private ITransaction _transaction;

        public Worker(ITransaction tx)
        {
            _transaction = tx;
        }

        public List<Account> ExecuteTransaction()
        {
            return new AccountManager().ExecuteTransactionAsync(_transaction);
        }
    }
}
