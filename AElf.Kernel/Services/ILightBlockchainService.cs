using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Services
{
    public interface ILightBlockchainService
    {
        Task<bool> HasHeaderAsync(int chainId, Hash blockHash);
        Task AddHeadersAsync(int chainId, IEnumerable<IBlockHeader> headers);
        Task<Chain> GetChainAsync(int chainId);
        Task<IBlockHeader> GetHeaderByHashAsync(int chainId, Hash blockHash);
        Task<IBlockHeader> GetIrreversibleHeaderByHeightAsync(int chainId, long height);
    }
}