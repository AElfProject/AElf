using System.Collections.Generic;
using AElf.Management.Helper;
using AElf.Management.Interfaces;
using AElf.Management.Models;
using k8s;
using k8s.Models;
using Microsoft.AspNetCore.JsonPatch;

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

        public void ModifyWorkerCount(string chainId, int workerCount)
        {
            var patch = new JsonPatchDocument<V1Deployment>();
            patch.Replace(e => e.Spec.Replicas, workerCount);
            K8SRequestHelper.GetClient().PatchNamespacedDeployment(new V1Patch(patch), "deploy-worker", chainId);
        }
    }
}