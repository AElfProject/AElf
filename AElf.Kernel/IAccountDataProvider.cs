using System.Threading.Tasks;

namespace AElf.Kernel
{

    public interface ISerializable
    {
        byte[] Serialize();
    }

    /// <summary>
    /// Data is stored associated with Account
    /// </summary>
    public interface IAccountDataProvider : ISerializable
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

        IHash<IAccount> GetAccountAddress();

        Task<IDataProvider> GetMapAsync(string name);
    }
}