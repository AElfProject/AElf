using System.Collections.Concurrent;
using System.Linq;
using k8s;

namespace AElf.Management.Helper
{
    public class ServiceUrlHelper
    {
        private static ConcurrentDictionary<string,string> RpcAddresses=new ConcurrentDictionary<string, string>();
        
        private static ConcurrentDictionary<string,string> MonitorRpcAddress=new ConcurrentDictionary<string, string>();
        
        public static string GetRpcAddress(string chainId)
        {
            if (RpcAddresses.ContainsKey(chainId))
            {
                return RpcAddresses[chainId];
            }

            var service = K8SRequestHelper.GetClient().ReadNamespacedService(GlobalSetting.LauncherServiceName, chainId);
            var address = "http://" + service.Status.LoadBalancer.Ingress.FirstOrDefault().Hostname + ":" + GlobalSetting.RpcPort;
            RpcAddresses.TryAdd(chainId, address);
            
            return address;
        }

        public static string GetMonitorRpcAddress(string chainId)
        {
            if (MonitorRpcAddress.ContainsKey(chainId))
            {
                return MonitorRpcAddress[chainId];
            }

            var service = K8SRequestHelper.GetClient().ReadNamespacedService(GlobalSetting.MonitorServiceName, chainId);
            var address = "http://" + service.Status.LoadBalancer.Ingress.FirstOrDefault().Hostname + ":" + GlobalSetting.MonitorPort;
            MonitorRpcAddress.TryAdd(chainId, address);
            
            return address;
        }
    }
}