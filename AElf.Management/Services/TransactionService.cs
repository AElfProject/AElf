using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Management.Helper;
using AElf.Management.Interfaces;
using AElf.Management.Models;
using AElf.Management.Request;

namespace AElf.Management.Services
{
    public class TransactionService:ITransactionService
    {
        public ulong GetPoolSize(string chainId)
        {
            var jsonRpcArg = new JsonRpcArg();
            jsonRpcArg.Method = "get_txpool_size";

            var state = HttpRequestHelper.Request<JsonRpcResult<TxPoolSizeResult>>(ServiceUrlHelper.GetRpcAddress(chainId)+"/chain", jsonRpcArg);

            return state.Result.CurrentTransactionPoolSize;
        }

        public void RecordPoolSize(string chainId, DateTime time, ulong poolSize)
        {
            var fields = new Dictionary<string, object> {{"size", poolSize}};
            InfluxDBHelper.Set(chainId, "transaction_pool_size", fields, null, time);
        }

        public List<PoolSizeHistory> GetPoolSizeHistory(string chainId)
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
    }
}