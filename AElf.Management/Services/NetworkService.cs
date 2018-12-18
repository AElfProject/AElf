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
    public class NetworkService:INetworkService
    {
        public async Task<PoolStateResult> GetPoolState(string chainId)
        {
            var jsonRpcArg = new JsonRpcArg();
            jsonRpcArg.Method = "get_pool_state";

            var state = await HttpRequestHelper.Request<JsonRpcResult<PoolStateResult>>(ServiceUrlHelper.GetRpcAddress(chainId)+"/net", jsonRpcArg);

            return state.Result;
        }

        public async Task<PeerResult> GetPeers(string chainId)
        {
            var jsonRpcArg = new JsonRpcArg();
            jsonRpcArg.Method = "get_peers";

            var peers = await HttpRequestHelper.Request<JsonRpcResult<PeerResult>>(ServiceUrlHelper.GetRpcAddress(chainId)+"/net", jsonRpcArg);
            
            return peers.Result;
        }

        public async Task RecordPoolState(string chainId, DateTime time, int requestPoolSize, int receivePoolSize)
        {
            var fields = new Dictionary<string, object> {{"request", requestPoolSize}, {"receive", receivePoolSize}};
            InfluxDBHelper.Set(chainId, "network_pool_state", fields, null, time);
        }

        public async Task<List<PoolStateHistory>> GetPoolStateHistory(string chainId)
        {
            var result = new List<PoolStateHistory>();
            var record = InfluxDBHelper.Get(chainId, "select * from network_pool_state");
            foreach (var item in record.First().Values)
            {
                result.Add(new PoolStateHistory
                {
                    Time = Convert.ToDateTime(item[0]),
                    ReceivePoolSize = Convert.ToInt32(item[1]),
                    RequestPoolSize = Convert.ToInt32(item[2])

                });
            }

            return result;
        }
    }
}