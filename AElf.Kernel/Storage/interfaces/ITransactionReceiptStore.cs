using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElf.Kernel.Storage.Interfaces
{
    public interface ITransactionReceiptStore
    {
        Task SetAsync(string key, object value);
        Task<T> GetAsync<T>(string key);
        Task<bool> PipelineSetAsync(Dictionary<string, object> pipelineSet);
    }
}