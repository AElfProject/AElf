using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Management.Helper;
using AElf.Management.Interfaces;
using AElf.Management.Models;
using k8s;

namespace AElf.Management.Services
{
    public class LighthouseService : ILighthouseService
    {
        public async Task<List<LighthouseResult>> GetAllLighthouses(string chainId)
        {
            var pods = await K8SRequestHelper.GetClient().ListNamespacedPodAsync(chainId, labelSelector: "name=" + GlobalSetting.LighthouseName);

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