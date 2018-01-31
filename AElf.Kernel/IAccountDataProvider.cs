using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    /// <summary>
    /// Data is stored associated with Account
    /// </summary>
    public interface IAccountDataProvider
    {
        /// <summary>
        /// Gets the data merkle tree root.
        /// </summary>
        /// <returns></returns>
        Task<IHash<IMerkleTree<ISerializable>>> GetDataMerkleTreeRootAsync();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task<ISerializable> GetAsync(IHash key);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        Task SetAsync(IHash key,ISerializable obj);
        
        IAccountDataContext Context { get; set; }


        IHash<IAccount> GetAccountAddress();

        Task<IAccountDataProvider> GetMapAsync(string name);
    }

    public interface IAccountDataContext
    {
        ulong IncreasementId { get; set; }
    }
}