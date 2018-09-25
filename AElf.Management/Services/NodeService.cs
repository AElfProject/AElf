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
    public class NodeService : INodeService
    {
        public bool IsAlive(string chainId)
        {
            var jsonRpcArg = new JsonRpcArg();
            jsonRpcArg.Method = "dpos_isalive";

            var state = HttpRequestHelper.Request<JsonRpcResult<DposStateResult>>(ServiceUrlHelper.GetRpcAddress(chainId)+"/chain", jsonRpcArg);

            return state.Result.IsAlive;
        }
        
        public bool IsForked(string chainId)
        {
            var jsonRpcArg = new JsonRpcArg();
            jsonRpcArg.Method = "node_isforked";

            var state = HttpRequestHelper.Request<JsonRpcResult<NodeStateResult>>(ServiceUrlHelper.GetRpcAddress(chainId)+"/chain", jsonRpcArg);

            return state.Result.IsForked;
        }
        
        public void RecordPoolState(string chainId, DateTime time, bool isAlive, bool isForked)
        {
            var fields = new Dictionary<string, object> {{"alive", isAlive}, {"forked", isForked}};
            InfluxDBHelper.Set(chainId, "node_state", fields, null, time);
        }

        public List<NodeStateHistory> GetHistoryState(string chainId)
        {
            var result = new List<NodeStateHistory>();
            var record = InfluxDBHelper.Get(chainId, "select * from node_state");
            foreach (var item in record.First().Values)
            {
                result.Add(new NodeStateHistory
                {
                    Time = Convert.ToDateTime(item[0]),
                    IsAlive = Convert.ToBoolean(item[1]),
                    IsForked = Convert.ToBoolean(item[2])             
                });
            }

            return result;
        }
    }
}