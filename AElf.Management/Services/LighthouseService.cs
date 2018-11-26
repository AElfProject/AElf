using System.Collections.Generic;
using AElf.Management.Helper;
using AElf.Management.Interfaces;
using AElf.Management.Models;
using k8s;

namespace AElf.Management.Services
{
    public class LighthouseService:ILighthouseService
    {
        public List<LighthouseResult> GetAllLighthouses(string chainId)
        {
            var pods = K8SRequestHelper.GetClient().ListNamespacedPod(chainId, labelSelector: "name=" + GlobalSetting.LighthouseName);

            var result = new List<LighthouseResult>();
            foreach (var pod in pods.Items)
            {
                result.Add(new LighthouseResult
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