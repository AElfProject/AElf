using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Services
{
    public interface ILightBlockchainService
    {
        Task<bool> HasHeaderAsync(int chainId, Hash blockHash);
        Task<bool> IsOnCanonical(int chainId, Hash blockId);
        Task AddHeadersAsync(int chainId, IEnumerable<IBlockHeader> headers);
        Task<ulong> GetCurrentBlockHeightAsync(int chainId);
        Task<Hash> GetCurrentBlockHashAsync(int chainId);
        Task<IBlockHeader> GetHeaderByHashAsync(int chainId,Hash blockHash);
        Task<IBlockHeader> GetHeaderByHeightAsync(int chainId, ulong height);
        Task<Hash> GetCanonicalHashAsync(ulong height);
    }
}