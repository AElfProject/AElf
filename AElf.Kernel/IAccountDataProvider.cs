using System.Threading.Tasks;

namespace AElf.Kernel
{
    /// <summary>
    /// Data is stored associated with Account
    /// </summary>
    public interface IAccountDataProvider
    {
        /// <summary>
        /// The merkle tree root of one account's data
        /// </summary>
        /// <returns></returns>
        Task<IHash<IMerkleTree<object>>> GetDataMerkleTreeRootAsync();


        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        Task<object> GetAsync(IHash address);

        /// <summary>
        /// </summary>
        /// <param name="address"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        Task SetAsync(IHash address,object obj);
        
        object Context { get; set; }
        
    }
    
    

}