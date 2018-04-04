using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    
    public interface ISmartContract :IHashProvider
    {
        Task InitializeAsync(IAccountDataProvider dataProvider);
        Task InvokeAsync(IHash caller, 
            string methodname, params object[] objs);
    }
}