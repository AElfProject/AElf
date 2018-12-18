using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Management.Helper;
using AElf.Management.Interfaces;
using AElf.Management.Models;
using AElf.Management.Request;

namespace AElf.Management.Services
{
    public class TransactionService:ITransactionService
    {
        public async Task RecordPoolSize(string chainId, DateTime time)
        {
            var poolSize = await GetPoolSize(chainId);
            
            var fields = new Dictionary<string, object> {{"size", poolSize}};
            InfluxDBHelper.Set(chainId, "transaction_pool_size", fields, null, time);
        }

        public async Task<List<PoolSizeHistory>> GetPoolSizeHistory(string chainId)
        {
            var result = new List<PoolSizeHistory>();
            var record = InfluxDBHelper.Get(chainId, "select * from transaction_pool_size");
            foreach (var item in record.First().Values)
            {
                result.Add(new PoolSizeHistory
                {
                    Time = Convert.ToDateTime(item[0]),
                    Size = Convert.ToUInt64(item[1])                    
                });
            }

            return result;
        }
        
        public async Task<ulong> GetPoolSize(string chainId)
        {
            var jsonRpcArg = new JsonRpcArg();
            jsonRpcArg.Method = "get_txpool_size";

            var state = await HttpRequestHelper.Request<JsonRpcResult<TxPoolSizeResult>>(ServiceUrlHelper.GetRpcAddress(chainId)+"/chain", jsonRpcArg);

            return state.Result.CurrentTransactionPoolSize;
        }
    }
}