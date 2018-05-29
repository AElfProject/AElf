using System;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IExecutive
    {
        IExecutive SetContext(ITransactionContext context);
        Task Apply();
    }
}
