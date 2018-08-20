using System.Collections.Generic;
using AElf.Management.Helper;
using AElf.Management.Models;
using k8s;

namespace AElf.Management
{
    public class ManagerService:IManagerService
    {
        public List<ManagerResult> GetAllManagers(string chainId)
        {
            var pods = K8SRequestHelper.GetClient().ListNamespacedPod(chainId, labelSelector: "name=set-manager");

            var result = new List<ManagerResult>();
            foreach (var pod in pods.Items)
            {
                result.Add(new ManagerResult
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