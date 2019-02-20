using System.Threading.Tasks;

namespace AElf.Kernel.Domain
{
    public interface IMinersManager
    {
        Task<Miners> GetMiners(ulong termNumber);
        Task SetMiners(Miners miners, int chainId);
        Task<bool> IsMinersInDatabase();
    }
}