using System.Threading.Tasks;

namespace AElf.Kernel.Managers
{
    public interface IMinersManager
    {
        Task<Miners> GetMiners();
        Task SetMiners(Miners miners);
    }
}