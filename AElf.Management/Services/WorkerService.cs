using System.Collections.Generic;
using AElf.Management.Helper;
using AElf.Management.Interfaces;
using AElf.Management.Models;
using k8s;

namespace AElf.Management.Services
{
    public class WorkerService : IWorkerService
    {
        public List<WorkerResult> GetAllWorkers(string chainId)
        {
            var pods = K8SRequestHelper.GetClient().ListNamespacedPod(chainId, labelSelector: "name=deploy-worker");

            var result = new List<WorkerResult>();
            foreach (var pod in pods.Items)
            {
                result.Add(new WorkerResult
                {
                    NameSpace = pod.Metadata.NamespaceProperty,
                    Name = pod.Metadata.Name,
                    Status = pod.Status.Phase,
                    CreateTime = pod.Metadata.CreationTimestamp
                });
            }

            return result;
        }
    }
}