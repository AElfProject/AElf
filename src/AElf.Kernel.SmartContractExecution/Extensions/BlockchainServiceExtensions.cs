using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;

namespace AElf.Kernel.SmartContractExecution
{
    public static class BlockchainServiceExtensions
    {
        public static async Task<List<Block>> GetBlocksAsync(this IBlockchainService blockchainService,
            IEnumerable<Hash> blockHashes)
        {
            var list = blockHashes
                .Select(async blockHash => await blockchainService.GetBlockByHashAsync(blockHash));

            return (await Task.WhenAll(list)).ToList();
        }
    }
}