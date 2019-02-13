using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Common;
using AElf.Kernel;
using AElf.Kernel.Managers;
using Google.Protobuf;

namespace AElf.SmartContract.Contexts
{
    internal class StateProvider : IStateProvider
    {
        public IBlockchainStateManager BlockchainStateManager { get; set; }

        // TODO: Combine SmartContractContext and TransactionContext
        public ITransactionContext TransactionContext { get; set; }
        public ISmartContractContext ContractContext { get; set; }

        public async Task<byte[]> GetAsync(StatePath path)
        {
            // TODO: StatePath (string)
            var byteString = await BlockchainStateManager.GetStateAsync(
                string.Join("/",
                    GetKeyEnumerable(ContractContext.ContractAddress, path.Path.Select(x => x.ToStringUtf8()))),
                TransactionContext.BlockHeight,
                TransactionContext.PreviousBlockHash
            );
            byteString = byteString ?? ByteString.Empty;
            return byteString.ToByteArray();
        }

        private static IEnumerable<string> GetKeyEnumerable(Address address, IEnumerable<string> path)
        {
            yield return address.GetFormatted();
            foreach (var part in path)
            {
                yield return part;
            }
        }
    }
}