using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel;
using AElf.Kernel.SmartContractExecution.Domain;
using AElf.Kernel.SmartContract;
using AElf.Kernel.SmartContract.Contexts;

namespace AElf.Sdk.CSharp2.Tests
{
    public class MockStateProvider : IStateProvider
    {
        private IStateManager _stateManager;
        public ITransactionContext TransactionContext { get; set; }
        public Dictionary<StatePath, StateCache> Cache { get; set; }
        public ISmartContractContext ContractContext { get; set; }

        public MockStateProvider(IStateManager stateManager)
        {
            _stateManager = stateManager;
        }

        public async Task<byte[]> GetAsync(StatePath path)
        {
            var output = await _stateManager.GetAsync(path);
            Console.WriteLine($"{path} {output}");
            return output;
        }
    }
}