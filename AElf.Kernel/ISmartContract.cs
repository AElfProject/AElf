using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    
    public interface ISmartContract
    {
        Task InitializeAsync(IAccountDataProvider dataProvider);
        Task InvokeAsync(IAccount caller, 
            string methodname, params object[] objs);
    }    
    
}