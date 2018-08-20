using System.Collections.Generic;
using AElf.Management.Models;

namespace AElf.Management
{
    public interface IWorkerService
    {
        List<WorkerResult> GetAllWorkers(string chainId);
    }
}