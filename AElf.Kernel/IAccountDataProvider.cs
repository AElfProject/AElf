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

        byte[] GetAccountAddress();

        IDataProvider GetDataProvider();
    }
}