using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.Kernel
{
    public interface ILightChain
    {
        Task<bool> HasHeader(Hash blockHash);
        Task<bool> IsOnCanonical(Hash blockId);
        Task AddHeadersAsync(IEnumerable<IBlockHeader> headers);
        Task<ulong> GetCurrentBlockHeightAsync();
        Task<Hash> GetCurrentBlockHashAsync();
        Task<IBlockHeader> GetHeaderByHashAsync(Hash blockHash);
        Task<IBlockHeader> GetHeaderByHeightAsync(ulong height);
    }
}