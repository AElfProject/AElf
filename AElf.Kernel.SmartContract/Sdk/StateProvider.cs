using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.SmartContract.Domain;
using AElf.Kernel.SmartContract.Sdk;
using AElf.Kernel.SmartContractExecution.Domain;
using Google.Protobuf;

namespace AElf.Kernel.SmartContract.Contexts
{
    public class NullStateCache : IStateCache
    {
        public bool TryGetValue(StatePath key, out byte[] value)
        {
            value = null;
            return false;
        }

        public byte[] this[StatePath key]
        {
            get => null;
            set { }
        }
    }

    internal class StateProvider : IStateProvider
    {
        public IBlockchainStateManager BlockchainStateManager { get; set; }

        // TODO: Combine SmartContractContext and TransactionContext
        public ITransactionContext TransactionContext { get; set; }
        public IStateCache Cache { get; set; } = new NullStateCache();

        public async Task<byte[]> GetAsync(StatePath path)
        {
            // TODO: StatePath (string)
            var byteString = await BlockchainStateManager.GetStateAsync(
                string.Join("/", path.Path.Select(x => x.ToStringUtf8())),
                TransactionContext.BlockHeight - 1,
                TransactionContext.PreviousBlockHash
            );
//            byteString = byteString ?? ByteString.Empty;
            return byteString?.ToByteArray();
        }
    }
}