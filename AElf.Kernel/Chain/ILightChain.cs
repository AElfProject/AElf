using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf.Kernel
{
    public interface ILightChain
    {
        Task<bool> HasHeader(Hash blockId);
        Task<bool> IsOnCanonical(Hash blockId);
        Task AddHeadersAsync(IEnumerable<IBlockHeader> headers);
        Task<Hash> GetCurrentBlockHashAsync();
        Task<IBlockHeader> GetHeaderByHashAsync(Hash blockId);
        Task<IBlockHeader> GetHeaderByHeightAsync(ulong height);
    }
}