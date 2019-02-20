using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.SmartContractExecution.Domain;

namespace AElf.Sdk.CSharp2.Tests
{
    /*
    public class MockStateManager : IStateManager
    {
        public Dictionary<StatePath, byte[]> Cache = new Dictionary<StatePath, byte[]>();

        public async Task SetAsync(StatePath path, byte[] value)
        {
            Cache[path] = value;
            await Task.CompletedTask;
        }

        public async Task<byte[]> GetAsync(StatePath path)
        {
            if (!Cache.TryGetValue(path, out var value))
            {
                value = new byte[0];
            }

            return await Task.FromResult(value);
        }

        public async Task PipelineSetAsync(Dictionary<StatePath, byte[]> pipelineSet)
        {
            foreach (var kv in pipelineSet)
            {
                await SetAsync(kv.Key, kv.Value);
            }
        }
    }
    */
}