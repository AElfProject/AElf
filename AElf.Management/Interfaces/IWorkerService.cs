using System.Collections.Generic;
using AElf.Management.Models;

namespace AElf.Management.Interfaces
{
    public interface IWorkerService
    {
        List<WorkerResult> GetAllWorkers(string chainId);

        void ModifyWorkerCount(string chainId, int workerCount);
    }
}