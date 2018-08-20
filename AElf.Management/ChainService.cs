using System.Collections.Generic;
using System.Linq;
using AElf.Management.Helper;
using AElf.Management.Models;
using k8s;

namespace AElf.Management
{
    public class ChainService : IChainService
    {
        public List<ChainResult> GetAllChains()
        {
            var namespaces = K8SRequestHelper.GetClient().ListNamespace();

            return (from np in namespaces.Items
                where np.Metadata.Name != "default" && np.Metadata.Name != "kube-system" && np.Metadata.Name != "kube-public"
                select new ChainResult
                {
                    ChainId = np.Metadata.Name,
                    Status = np.Status.Phase,
                    CreateTime = np.Metadata.CreationTimestamp
                }).ToList();
        }
    }
}