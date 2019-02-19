using System.Threading.Tasks;
using AElf.Common;

namespace AElf.Kernel.Managers
{
    public interface IMinersManager
    {
        Task<Miners> GetMiners(ulong termNumber);
        Task SetMiners(Miners miners);
        Task<bool> IsMinersInDatabase();
    }
}