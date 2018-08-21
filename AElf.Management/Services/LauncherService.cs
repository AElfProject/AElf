using System.Collections.Generic;
using AElf.Management.Helper;
using AElf.Management.Interfaces;
using AElf.Management.Models;
using k8s;

namespace AElf.Management.Services
{
    public class LauncherService:ILauncherService
    {
        public List<LauncherResult> GetAllLaunchers(string chainId)
        {
            var pods = K8SRequestHelper.GetClient().ListNamespacedPod(chainId, labelSelector: "name=deploy-launcher");

            var result = new List<LauncherResult>();
            foreach (var pod in pods.Items)
            {
                result.Add(new LauncherResult
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