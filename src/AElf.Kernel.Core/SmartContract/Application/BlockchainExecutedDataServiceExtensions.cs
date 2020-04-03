using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Types;
using Google.Protobuf;

namespace AElf.Kernel.SmartContract.Application
{
    public static class BlockchainExecutedDataServiceExtensions
    {
        public static async Task<T> GetBlockExecutedDataAsync<T>(
            this IBlockchainExecutedDataService blockchainExecutedDataService,
            IBlockIndex chainContext, string key)
        {
            var byteString = await blockchainExecutedDataService.GetBlockExecutedDataAsync(chainContext, key);
            return SerializationHelper.Deserialize<T>(byteString?.ToByteArray());
        }

        public static async Task AddBlockExecutedDataAsync<T>(
            this IBlockchainExecutedDataService blockchainExecutedDataService,
            Hash blockHash, string key, T blockExecutedData)
        {
            var dic = new Dictionary<string, ByteString>
            {
                {key, ByteString.CopyFrom(SerializationHelper.Serialize(blockExecutedData))}
            };
            await blockchainExecutedDataService.AddBlockExecutedDataAsync(blockHash, dic);
        }

        public static async Task AddBlockExecutedDataAsync<T>(
            this IBlockchainExecutedDataService blockchainExecutedDataService,
            Hash blockHash, IDictionary<string, T> blockExecutedData)
        {
            var dic = blockExecutedData.ToDictionary(
                keyPair => keyPair.Key,
                keyPair => ByteString.CopyFrom(SerializationHelper.Serialize(keyPair.Value)));
            await blockchainExecutedDataService.AddBlockExecutedDataAsync(blockHash, dic);
        }
    }
}