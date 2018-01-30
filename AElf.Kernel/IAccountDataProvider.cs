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
    public interface IAccountDataProvider
    {
        /// <summary>
        /// Gets the data merkle tree root.
        /// </summary>
        /// <returns></returns>
        Task<IHash<IMerkleTree<ISerializable>>> GetDataMerkleTreeRootAsync();

        /// <summary>
        /// Gets the async.
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        Task<ISerializable> GetAsync(IHash address);

        /// <summary>
        /// </summary>
        /// <param name="address"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        Task SetAsync(IHash address,ISerializable obj);
        
        IAccountDataContext Context { get; set; }


        IHash<IAccount> GetAccountAddress();
    }

    public interface IAccountDataContext
    {
        long IncreasementId { get; set; }
    }
}