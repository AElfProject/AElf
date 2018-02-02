using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    /// <summary>
    /// Data is stored associated with Account
    /// </summary>
    public interface IAccountDataProvider : ISerializable
    {

        IAccountDataContext Context { get; set; }

        IHash<IAccount> GetAccountAddress();

        IDataProvider GetDataProvider();
    }
}