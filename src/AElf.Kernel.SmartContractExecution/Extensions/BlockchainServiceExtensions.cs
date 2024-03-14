using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Kernel.Blockchain.Application;
using AElf.Types;

namespace AElf.Kernel.SmartContractExecution;

public static class BlockchainServiceExtensions
{
    public static async Task<List<Block>> GetBlocksAsync(this IBlockchainService blockchainService,
        IEnumerable<Hash> blockHashes)
    {
        var tasks = blockHashes.Select(blockchainService.GetBlockByHashAsync);
        var blocks = await Task.WhenAll(tasks);
        return blocks.ToList();
    }
}