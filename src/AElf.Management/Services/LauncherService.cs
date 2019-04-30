using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Management.Helper;
using AElf.Management.Interfaces;
using AElf.Management.Models;
using k8s;
using k8s.Models;

namespace AElf.Management.Services
{
    public class LauncherService : ILauncherService
    {
        public async Task<List<LauncherResult>> GetAllLaunchers(string chainId)
        {
            var pods = await K8SRequestHelper.GetClient().ListNamespacedPodAsync(chainId, labelSelector: "name=" + GlobalSetting.LauncherName);

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