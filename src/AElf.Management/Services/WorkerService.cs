using System.Collections.Generic;
using System.Threading.Tasks;
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
        public async Task<List<WorkerResult>> GetAllWorkers(string chainId)
        {
            var configs = await K8SRequestHelper.GetClient().ReadNamespacedConfigMapAsync(GlobalSetting.CommonConfigName, chainId);

            var pods = await K8SRequestHelper.GetClient().ListNamespacedPodAsync(chainId, labelSelector: "name=" + GlobalSetting.WorkerName);

            var result = new List<WorkerResult>();
            foreach (var pod in pods.Items)
            {
                result.Add(new WorkerResult
                {
                    NameSpace = pod.Metadata.NamespaceProperty,
                    Name = pod.Metadata.Name,
                    Status = pod.Status.Phase,
                    CreateTime = pod.Metadata.CreationTimestamp,
                });
            }

            return result;
        }

        public async Task ModifyWorkerCount(string chainId, int workerCount)
        {
            var patch = new JsonPatchDocument<V1Deployment>();
            patch.Replace(e => e.Spec.Replicas, workerCount);
            await K8SRequestHelper.GetClient().PatchNamespacedDeploymentAsync(new V1Patch(patch), GlobalSetting.WorkerName, chainId);
        }
    }
}