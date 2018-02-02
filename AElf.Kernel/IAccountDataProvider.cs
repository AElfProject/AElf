using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    /// <summary>
    /// Data is stored associated with Account
    /// </summary>
    public interface IAccountDataProvider
    {
        
        
        IAccountDataContext Context { get; set; }


        IHash<IAccount> GetAccountAddress();

        Task<IAccountDataProvider> GetMapAsync(string name);

        IDataProvider GetDataProvider();
    }
}