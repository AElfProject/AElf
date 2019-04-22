using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Management.Models;

namespace AElf.Management.Interfaces
{
    public interface IWorkerService
    {
        Task<List<WorkerResult>> GetAllWorkers(string chainId);

        Task ModifyWorkerCount(string chainId, int workerCount);
    }
}