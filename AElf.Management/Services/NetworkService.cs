using System;
using System.Collections.Generic;
using System.Linq;
using AElf.Management.Database;
using AElf.Management.Helper;
using AElf.Management.Interfaces;
using AElf.Management.Models;
using AElf.Management.Request;

namespace AElf.Management.Services
{
    public class NetworkService:INetworkService
    {
        public PoolStateResult GetPoolState(string chainId)
        {
            var jsonRpcArg = new JsonRpcArg();
            jsonRpcArg.Method = "get_pool_state";

            var state = HttpRequestHelper.Request<JsonRpcResult<PoolStateResult>>(ServiceUrlHelper.GetRpcAddress(chainId)+"/net", jsonRpcArg);

            return state.Result;
        }

        public PeerResult GetPeers(string chainId)
        {
            var jsonRpcArg = new JsonRpcArg();
            jsonRpcArg.Method = "get_peers";

            var peers = HttpRequestHelper.Request<JsonRpcResult<PeerResult>>(ServiceUrlHelper.GetRpcAddress(chainId)+"/net", jsonRpcArg);
            
            return peers.Result;
        }

        public void RecordPoolState(string chainId, DateTime time, int requestPoolSize, int receivePoolSize)
        {
            var fields = new Dictionary<string, object> {{"request", requestPoolSize}, {"receive", receivePoolSize}};
            InfluxDBHelper.Set(chainId, "network_pool_state", fields, null, time);
        }

        public List<PoolStateHistory> GetPoolStateHistory(string chainId)
        {
            var result = new List<PoolStateHistory>();
            var record = InfluxDBHelper.Get(chainId, "select * from node_state");
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