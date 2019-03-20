using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Management.Database;
using AElf.Management.Helper;
using AElf.Management.Interfaces;
using AElf.Management.Models;
using AElf.Management.Request;
using Microsoft.Extensions.Options;

namespace AElf.Management.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly ManagementOptions _managementOptions;
        private readonly IInfluxDatabase _influxDatabase;

        public TransactionService(IOptionsSnapshot<ManagementOptions> options,IInfluxDatabase influxDatabase)
        {
            _managementOptions = options.Value;
            _influxDatabase = influxDatabase;
        }

        public async Task RecordPoolSize(string chainId, DateTime time)
        {
            var poolSize = await GetPoolSize(chainId);

            var fields = new Dictionary<string, object> {{"size", poolSize}};
            await _influxDatabase.Set(chainId, "transaction_pool_size", fields, null, time);
        }

        public async Task<List<PoolSizeHistory>> GetPoolSizeHistory(string chainId)
        {
            var result = new List<PoolSizeHistory>();
            var record = await _influxDatabase.Get(chainId, "select * from transaction_pool_size");
            foreach (var item in record.First().Values)
            {
                result.Add(new PoolSizeHistory
                {
                    Time = Convert.ToDateTime(item[0]),
                    Size = Convert.ToUInt32(item[1])
                });
            }

            return result;
        }

        public async Task<int> GetPoolSize(string chainId)
        {
            var jsonRpcArg = new JsonRpcArg {Method = "GetTransactionPoolStatus"};

            var state = await HttpRequestHelper.Request<JsonRpcResult<TxPoolSizeResult>>(
                _managementOptions.ServiceUrls[chainId].RpcAddress + "/chain", jsonRpcArg);

            return state.Result.Queued;
        }
    }
}