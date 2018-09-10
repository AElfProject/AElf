using System.Collections.Generic;
using AElf.Configuration;
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
            var configs = K8SRequestHelper.GetClient().ReadNamespacedConfigMap(GlobalSetting.CommonConfigName, chainId);
            var configName = GetConfigName<ActorConfig>();
            var actorConfig = JsonSerializer.Instance.Deserialize<ActorConfig>(configs.Data[configName]); 

            var pods = K8SRequestHelper.GetClient().ListNamespacedPod(chainId, labelSelector: "name=" + GlobalSetting.WorkerName);

            var result = new List<WorkerResult>();
            foreach (var pod in pods.Items)
            {
                result.Add(new WorkerResult
                {
                    NameSpace = pod.Metadata.NamespaceProperty,
                    Name = pod.Metadata.Name,
                    Status = pod.Status.Phase,
                    CreateTime = pod.Metadata.CreationTimestamp,
                    ActorCount = actorConfig.ActorCount
                });
            }

            return result;
        }
        
        private static string GetConfigName<T>()
        {
            var t = typeof(T);
            var attrs = t.GetCustomAttributes(typeof(ConfigFileAttribute), false);
            return attrs.Length > 0 ? ((ConfigFileAttribute) attrs[0]).FileName : t.Name;
        }

        public void ModifyWorkerCount(string chainId, int workerCount)
        {
            var patch = new JsonPatchDocument<V1Deployment>();
            patch.Replace(e => e.Spec.Replicas, workerCount);
            K8SRequestHelper.GetClient().PatchNamespacedDeployment(new V1Patch(patch), GlobalSetting.WorkerName, chainId);
        }
    }
}