using System.Threading.Tasks;
using Orleans;

namespace AElf.Kernel.SmartContract.Parallel.Orleans.Application
{
    public interface IClusterClientService
    {
        IClusterClient Client { get; }
        Task StartAsync();
        Task StopAsync();
    }
}