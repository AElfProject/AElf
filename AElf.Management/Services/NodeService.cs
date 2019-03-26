using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf.Management.Database;
using AElf.Management.Helper;
using AElf.Management.Interfaces;
using AElf.Management.Models;
using AElf.Management.Request;
using Microsoft.Extensions.Options;

namespace AElf.Management.Services
{
    public class NodeService : INodeService
    {
        private readonly ManagementOptions _managementOptions;
        private readonly IInfluxDatabase _influxDatabase;

        public NodeService(IOptionsSnapshot<ManagementOptions> options, IInfluxDatabase influxDatabase)
        {
            _managementOptions = options.Value;
            _influxDatabase = influxDatabase;
        }

        public async Task<bool> IsAlive(string chainId)
        {
            var jsonRpcArg = new JsonRpcArg();
            jsonRpcArg.Method = "GetDposStatus";

            var state = await HttpRequestHelper.Request<JsonRpcResult<DposStateResult>>(
                _managementOptions.ServiceUrls[chainId].RpcAddress + "/chain", jsonRpcArg);

            return state.Result.IsAlive;
        }

        public async Task<bool> IsForked(string chainId)
        {
            var jsonRpcArg = new JsonRpcArg();
            jsonRpcArg.Method = "GetNodeStatus";

            var state = await HttpRequestHelper.Request<JsonRpcResult<NodeStateResult>>(
                _managementOptions.ServiceUrls[chainId].RpcAddress + "/chain", jsonRpcArg);

            return state.Result.IsForked;
        }

        public async Task RecordPoolState(string chainId, DateTime time)
        {
            var isAlive = await IsAlive(chainId);
            var isForked = await IsForked(chainId);

            var fields = new Dictionary<string, object> {{"alive", isAlive}, {"forked", isForked}};
            await _influxDatabase.Set(chainId, "node_state", fields, null, time);
        }

        public async Task<List<NodeStateHistory>> GetHistoryState(string chainId)
        {
            var result = new List<NodeStateHistory>();
            var record = await _influxDatabase.Get(chainId, "select * from node_state");
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
            long currentHeight;
            var currentRecord = await _influxDatabase.Get(chainId, "select last(height) from block_info");
            if (currentRecord.Count == 0)
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
                    currentHeight = Convert.ToInt64(record[1]) + 1;
                }
            }

            var blockInfo = await GetBlockInfo(chainId, currentHeight);
            while (blockInfo != null && blockInfo.Body != null && blockInfo.Header != null)
            {
                var fields = new Dictionary<string, object>
                    {{"height", currentHeight}, {"tx_count", blockInfo.Body.TransactionsCount}};
                await _influxDatabase.Set(chainId, "block_info", fields, null, blockInfo.Header.Time);

                Thread.Sleep(1000);

                currentHeight++;
                blockInfo = await GetBlockInfo(chainId, currentHeight);
            }
        }

        public async Task RecordInvalidBlockCount(string chainId, DateTime time)
        {
            var count = await GetInvalidBlockCount(chainId);

            var fields = new Dictionary<string, object> {{"count", count}};
            await _influxDatabase.Set(chainId, "block_invalid", fields, null, time);
        }
        
        public async Task RecordGetCurrentChainStatus(string chainId, DateTime time)
        {
            var count = await GetCurrentChainStatus(chainId);

            var fields = new Dictionary<string, object> {{"LastIrrever", count.LastIrreversibleBlockHeight},{"Longest", count.LongestChainHeight},{"Best", count.BestChainHeight}};
            await _influxDatabase.Set(chainId, "block_status", fields, null, time);
        }

        public async Task RecordRollBackTimes(string chainId, DateTime time)
        {
            var times = await GetRollBackTimes(chainId);

            var fields = new Dictionary<string, object> {{"times", times}};
            await _influxDatabase.Set(chainId, "chain_rollback", fields, null, time);
        }

        private async Task<int> GetInvalidBlockCount(string chainId)
        {
            var jsonRpcArg = new JsonRpcArg();
            jsonRpcArg.Method = "get_invalid_block";

            var state = await HttpRequestHelper.Request<JsonRpcResult<InvalidBlockResult>>(
                _managementOptions.ServiceUrls[chainId].RpcAddress + "/chain", jsonRpcArg);

            return state.Result.InvalidBlockCount;
        }

        private async Task<int> GetRollBackTimes(string chainId)
        {
            var jsonRpcArg = new JsonRpcArg();
            jsonRpcArg.Method = "get_rollback_times";

            var state = await HttpRequestHelper.Request<JsonRpcResult<RollBackResult>>(
                _managementOptions.ServiceUrls[chainId].RpcAddress + "/chain", jsonRpcArg);

            return state.Result.RollBackTimes;
        }

        private async Task<BlockInfoResult> GetBlockInfo(string chainId, long height)
        {
            var jsonRpcArg = new JsonRpcArg<BlockInfoArg>();
            jsonRpcArg.Method = "GetBlockInfo";
            jsonRpcArg.Params = new BlockInfoArg
            {
                BlockHeight = height,
                IncludeTxs = false
            };

            var blockInfo =
                await HttpRequestHelper.Request<JsonRpcResult<BlockInfoResult>>(
                    _managementOptions.ServiceUrls[chainId].RpcAddress + "/chain", jsonRpcArg);

            return blockInfo.Result;
        }

        private async Task<long> GetCurrentChainHeight(string chainId)
        {
            var jsonRpcArg = new JsonRpcArg();
            jsonRpcArg.Method = "GetBlockHeight";

            var height =
                await HttpRequestHelper.Request<JsonRpcResult<int>>(
                    _managementOptions.ServiceUrls[chainId].RpcAddress + "/chain", jsonRpcArg);

            return height.Result;
        } 
         
        private async Task<ChainStatusResult> GetCurrentChainStatus(string chainId)
        {
            var jsonRpcArg = new JsonRpcArg();
            jsonRpcArg.Method = "GetChainStatus";

            var height =
                await HttpRequestHelper.Request<JsonRpcResult<ChainStatusResult>>(
                    _managementOptions.ServiceUrls[chainId].RpcAddress + "/chain", jsonRpcArg);

            return height.Result;
        }
    }
}