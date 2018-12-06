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
    public class NodeService : INodeService
    {
        public async Task<bool> IsAlive(string chainId)
        {
            var jsonRpcArg = new JsonRpcArg();
            jsonRpcArg.Method = "dpos_isalive";

            var state = await HttpRequestHelper.Request<JsonRpcResult<DposStateResult>>(ServiceUrlHelper.GetRpcAddress(chainId)+"/chain", jsonRpcArg);

            return state.Result.IsAlive;
        }
        
        public async Task<bool> IsForked(string chainId)
        {
            var jsonRpcArg = new JsonRpcArg();
            jsonRpcArg.Method = "node_isforked";

            var state = await HttpRequestHelper.Request<JsonRpcResult<NodeStateResult>>(ServiceUrlHelper.GetRpcAddress(chainId)+"/chain", jsonRpcArg);

            return state.Result.IsForked;
        }
        
        public async Task RecordPoolState(string chainId, DateTime time)
        {
            var isAlive = await IsAlive(chainId);
            var isForked = await IsForked(chainId);
            
            var fields = new Dictionary<string, object> {{"alive", isAlive}, {"forked", isForked}};
            InfluxDBHelper.Set(chainId, "node_state", fields, null, time);
        }

        public async Task<List<NodeStateHistory>> GetHistoryState(string chainId)
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

        public async Task RecordBlockInfo(string chainId)
        {
            ulong currentHeight;
            var currentRecord = InfluxDBHelper.Get(chainId, "select last(height) from block_info");
            if (currentRecord.Count==0)
            {
                currentHeight = await GetCurrentChainHeight(chainId);
            }
            else
            {
                var record = currentRecord.First().Values.First();
                var time = Convert.ToDateTime(record[0]);

                if (time < DateTime.Now.AddHours(-1))
                {
                    currentHeight = await GetCurrentChainHeight(chainId);
                }
                else
                {
                    currentHeight = Convert.ToUInt64(record[1]) + 1;
                }
            }

            var blockInfo = await GetBlockInfo(chainId,currentHeight);
            while (blockInfo.Result != null && blockInfo.Result.Body!=null && blockInfo.Result.Header !=null)
            {
                var fields = new Dictionary<string, object> {{"height", currentHeight}, {"tx_count", blockInfo.Result.Body.TransactionsCount}};
                InfluxDBHelper.Set(chainId, "block_info", fields, null, blockInfo.Result.Header.Time);

                currentHeight++;
                blockInfo = await GetBlockInfo(chainId,currentHeight);
            }
        }

        public async Task RecordInvalidBlockCount(string chainId,DateTime time)
        {
            var count = await GetInvalidBlockCount(chainId);
            
            var fields = new Dictionary<string, object> {{"count", count}};
            InfluxDBHelper.Set(chainId, "block_invalid", fields, null, time);
        }
        
        public async Task RecordRollBackTimes(string chainId,DateTime time)
        {
            var times = await GetRollBackTimes(chainId);
            
            var fields = new Dictionary<string, object> {{"times", times}};
            InfluxDBHelper.Set(chainId, "chain_rollback", fields, null, time);
        }
        
        private async Task<int> GetInvalidBlockCount(string chainId)
        {
            var jsonRpcArg = new JsonRpcArg();
            jsonRpcArg.Method = "get_invalid_block";

            var state = await HttpRequestHelper.Request<JsonRpcResult<InvalidBlockResult>>(ServiceUrlHelper.GetRpcAddress(chainId)+"/chain", jsonRpcArg);

            return state.Result.InvalidBlockCount;
        }

        private async Task<int> GetRollBackTimes(string chainId)
        {
            var jsonRpcArg = new JsonRpcArg();
            jsonRpcArg.Method = "get_rollback_times";

            var state = await HttpRequestHelper.Request<JsonRpcResult<RollBackResult>>(ServiceUrlHelper.GetRpcAddress(chainId)+"/chain", jsonRpcArg);

            return state.Result.RollBackTimes;
        }

        private async Task<BlockInfoResult> GetBlockInfo(string chainId, ulong height)
        {
            var jsonRpcArg = new JsonRpcArg<BlockInfoArg>();
            jsonRpcArg.Method = "get_block_info";
            jsonRpcArg.Params = new BlockInfoArg
            {
                BlockHeight = height,
                IncludeTxs = false
            };

            var blockInfo = await HttpRequestHelper.Request<JsonRpcResult<BlockInfoResult>>(ServiceUrlHelper.GetRpcAddress(chainId)+"/chain", jsonRpcArg);
            
            return blockInfo.Result;
        }

        private async Task<ulong> GetCurrentChainHeight(string chainId)
        {
            var jsonRpcArg = new JsonRpcArg();
            jsonRpcArg.Method = "get_block_height";

            var height = await HttpRequestHelper.Request<JsonRpcResult<ChainHeightResult>>(ServiceUrlHelper.GetRpcAddress(chainId)+"/chain", jsonRpcArg);

            return Convert.ToUInt64(height.Result.Result.ChainHeight);
        }
    }
}