using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Management.Database;
using AElf.Management.Interfaces;
using AElf.Management.Models;
using AElf.Management.Request;
using Microsoft.Extensions.Options;

namespace AElf.Management.Services
{
    public class NetworkService : INetworkService
    {
        private readonly ManagementOptions _managementOptions;
        private readonly IInfluxDatabase _influxDatabase;

        public NetworkService(IOptionsSnapshot<ManagementOptions> options, IInfluxDatabase influxDatabase)
        {
            _managementOptions = options.Value;
            _influxDatabase = influxDatabase;
        }

//        public async Task<PoolStateResult> GetPoolState(string chainId)
//        {
//            var jsonRpcArg = new JsonRpcArg();
//            jsonRpcArg.Method = "get_pool_state";
//
//            var state = await HttpRequestHelper.Request<JsonRpcResult<PoolStateResult>>(
//                _managementOptions.ServiceUrls[chainId].RpcAddress + "/net", jsonRpcArg);
//
//            return state.Result;
//        }

        public async Task<PeerResult> GetPeers(string chainId)
        {
            var jsonRpcArg = new JsonRpcArg();
            jsonRpcArg.Method = "GetPeers";

            var peers = await HttpRequestHelper.Request<JsonRpcResult<PeerResult>>(
                _managementOptions.ServiceUrls[chainId].RpcAddress + "/net", jsonRpcArg);

            return peers.Result;
        }

        public async Task RecordPoolState(string chainId, DateTime time, int requestPoolSize, int receivePoolSize)
        {
            var fields = new Dictionary<string, object> {{"request", requestPoolSize}, {"receive", receivePoolSize}};
            await _influxDatabase.Set(chainId, "network_pool_state", fields, null, time);
        }

        public async Task<List<PoolStateHistory>> GetPoolStateHistory(string chainId)
        {
            var result = new List<PoolStateHistory>();
            var record = await _influxDatabase.Get(chainId, "select * from network_pool_state");
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