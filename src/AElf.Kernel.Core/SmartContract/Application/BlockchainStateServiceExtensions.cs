using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel.SmartContract.Application
{
    public static class BlockchainStateServiceExtensions
    {
        public static async Task AddBlockExecutedDataAsync<T>(this IBlockchainStateService blockchainStateService, 
            Hash blockHash, string key, T blockExecutedData)
        {
            var dic = new Dictionary<string, ByteString>
            {
                {key, ByteString.CopyFrom(SerializationHelper.Serialize(blockExecutedData))}
            };
            await blockchainStateService.AddBlockExecutedDataAsync(blockHash, dic);
        }

        public static async Task AddBlockExecutedDataAsync<T>(this IBlockchainStateService blockchainStateService,
            Hash blockHash, IDictionary<string, T> blockExecutedData)
        {
            var dic = blockExecutedData.ToDictionary(
                keyPair => keyPair.Key,
                keyPair => ByteString.CopyFrom(SerializationHelper.Serialize(keyPair.Value)));
            await blockchainStateService.AddBlockExecutedDataAsync(blockHash, dic);
        }
    }
}