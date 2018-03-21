using AElf.Kernel.Merkle;
using System.Threading.Tasks;

namespace AElf.Kernel
{
    /// <summary>
    /// World State presents the state of a chain, changed by block. 
    /// </summary>
    public interface IWorldState
    {
        /// <summary>
        /// Get a data provider from the accounts address
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        //IAccountDataProvider GetAccountDataProviderByAccount(IAccount account);
        
        /// <summary>
        /// The merkle tree root presents the world state of a chain
        /// </summary>
        /// <returns></returns>
        Hash GetWorldStateMerkleTreeRoot();
        
        /// <summary>
        /// Add an accountDataProvider
        /// </summary>
        /// <param name="accountZeroDataProvider"></param>
        void AddAccountDataProvider(IAccountDataProvider accountZeroDataProvider);

        /// <summary>
        /// Add a dataProvider
        /// </summary>
        /// <param name="dataProvider"></param>
        void AddDataProvider(IDataProvider dataProvider);

        /// <summary>
        /// Update a dataProvider with a new one
        /// </summary>
        /// <param name="beforeAdd"></param>
        /// <param name="dataProvider"></param>
        void UpdateDataProvider(IDataProvider beforeAdd, IDataProvider dataProvider);

    }
    
    
}