using System;
using System.Collections.Generic;
using System.Linq;
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

        public void RecordBlockInfo(string chainId)
        {
            ulong currentHeight;
            var currentRecord = InfluxDBHelper.Get(chainId, "select last(height) from block_info");
            if (currentRecord.Count==0)
            {
                currentHeight = GetCurrentChainHeight(chainId);
            }
            else
            {
                var record = currentRecord.First().Values.First();
                var time = Convert.ToDateTime(record[0]);

                if (time < DateTime.Now.AddHours(-1))
                {
                    currentHeight = GetCurrentChainHeight(chainId);
                }
                else
                {
                    currentHeight = Convert.ToUInt64(record[1]) + 1;
                }
            }

            var blockInfo = GetBlockInfo(chainId,currentHeight);
            while (blockInfo.Result != null && blockInfo.Result.Body!=null && blockInfo.Result.Header !=null)
            {
                var fields = new Dictionary<string, object> {{"height", currentHeight}, {"tx_count", blockInfo.Result.Body.TransactionsCount}};
                InfluxDBHelper.Set(chainId, "block_info", fields, null, blockInfo.Result.Header.Time);

                currentHeight++;
                blockInfo = GetBlockInfo(chainId,currentHeight);
            }
        }

        private BlockInfoResult GetBlockInfo(string chainId, ulong height)
        {
            var jsonRpcArg = new JsonRpcArg<BlockInfoArg>();
            jsonRpcArg.Method = "get_block_info";
            jsonRpcArg.Params = new BlockInfoArg
            {
                BlockHeight = height,
                IncludeTxs = false
            };

            var blockInfo = HttpRequestHelper.Request<JsonRpcResult<BlockInfoResult>>(ServiceUrlHelper.GetRpcAddress(chainId)+"/chain", jsonRpcArg);
            
            return blockInfo.Result;
        }

        private ulong GetCurrentChainHeight(string chainId)
        {
            var jsonRpcArg = new JsonRpcArg();
            jsonRpcArg.Method = "get_block_height";

            var height = HttpRequestHelper.Request<JsonRpcResult<ChainHeightResult>>(ServiceUrlHelper.GetRpcAddress(chainId)+"/chain", jsonRpcArg);

            return Convert.ToUInt64(height.Result.Result.ChainHeight);
        }
    }
}