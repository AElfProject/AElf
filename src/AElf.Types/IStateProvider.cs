using System.Threading.Tasks;
using AElf.Kernel;

namespace AElf
{
    public interface IStateProvider
    {
        Task<byte[]> GetAsync(StatePath path);
    }
}