using System.Threading.Tasks;
using AElf.Types;

namespace AElf
{
    public interface IStateProvider
    {
        Task<byte[]> GetAsync(StatePath path);
    }
}