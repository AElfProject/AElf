using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    
    public interface ISmartContract
    {
        Task InititalizeAsync(IAccountDataProvider dataProvider);
        Task InvokeAsync(IHash<IAccount> caller, 
            string methodname, params object[] objs);
    }    
    
}