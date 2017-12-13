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
        /// <returns>The data merkle tree root.</returns>
        IHash<IMerkleTree<ISerializable>> GetDataMerkleTreeRoot();

        /// <summary>
        /// Gets the async.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="key">Key.</param>
        Task<ISerializable> GetAsync(IHash key);

        /// <summary>
        /// Sets the async.
        /// </summary>
        /// <returns>The async.</returns>
        /// <param name="key">Key.</param>
        /// <param name="obj">Object.</param>
        Task SetAsync(IHash key,ISerializable obj);
    }
}