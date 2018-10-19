using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;
using Google.Protobuf;
using AElf.Common;

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

        Task SetAsync<T>(Hash keyHash, byte[] obj) where T : IMessage, new();

        Task<byte[]> GetAsync<T>(Hash keyHash) where T : IMessage, new();

        Dictionary<StatePath, StateValue> GetChanges();

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