using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IWorker
    {
        List<Account> ExecuteTransaction();
    }
}
