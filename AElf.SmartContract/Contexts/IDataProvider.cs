using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using Google.Protobuf;

// ReSharper disable once CheckNamespace
namespace AElf.SmartContract
{
    /// <summary>
    /// A DataProvider is used to access database and will cause changes.
    /// </summary>
    public interface IDataProvider
    {
        /// <summary>
        /// Get sub DataProvider instance using data provider key.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        IDataProvider GetDataProvider(string name);

        /// <summary>
        /// Set pointer and data to database.
        /// </summary>
        /// <param name="keyHash"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        Task SetAsync<T>(Hash keyHash, byte[] obj) where T : IMessage, new();


        Task<byte[]> GetAsync<T>(Hash keyHash) where T : IMessage, new();

        /// <summary>
        /// Get data from database.
        /// </summary>
        /// <param name="keyHash"></param>
        /// <returns></returns>
        Task<T> GetDataAsync<T>(Hash keyHash) where T : IMessage, new();

        Task SetDataAsync<T>(Hash keyHash, T obj) where T : IMessage, new();
        /// <summary>
        /// DataProvider hash + Key hash
        /// </summary>
        /// <param name="keyHash"></param>
        /// <returns></returns>
        Hash GetPathFor(Hash keyHash);
        
        /// <summary>
        /// Failed to get current round number. 
        /// </summary>
        /// <returns></returns>
        IEnumerable<StateValueChange> GetValueChanges();
        
        /// <summary>
        /// Injected from outside for entry data provider of the executive (in worker actor)
        /// </summary>
        Dictionary<DataPath, StateCache> StateCache { get; set; }
        
        /// <summary>
        /// Clear cache of this instance and sub DataProviders instance.
        /// </summary>
        void ClearCache();
    }
}