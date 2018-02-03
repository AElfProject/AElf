using System.Threading.Tasks;

namespace AElf.Kernel
{
    public interface IDataProvider
    {
        IDataProvider GetDataProvider(string name);
        
        
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
        
        /// <summary>
        /// Gets the data merkle tree root.
        /// </summary>
        /// <returns></returns>
        Task<IHash<IMerkleTree<ISerializable>>> GetDataMerkleTreeRootAsync();

    }
}