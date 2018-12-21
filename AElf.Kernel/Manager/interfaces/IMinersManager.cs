using System.Threading.Tasks;

namespace AElf.Kernel.Manager.Interfaces
{
    public interface IMinersManager
    {
        Task<Miners> GetMiners();
        Task SetMiners(Miners miners);
        Task<bool> IsMinersInDatabase();
    }
}