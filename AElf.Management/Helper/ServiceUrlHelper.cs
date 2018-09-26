using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using AElf.Common.Application;
using AElf.Configuration;
using AElf.Configuration.Config.Management;
using k8s;

namespace AElf.Management.Helper
{
    public class ServiceUrlHelper
    {
        private static object _lock = new object();
        
        public static string GetRpcAddress(string chainId)
        {
            if (ServiceUrlConfig.Instance.ServiceUrls.TryGetValue(chainId, out var serviceUrl))
            {
                if (!string.IsNullOrWhiteSpace(serviceUrl.RpcAddress))
                {
                    return serviceUrl.RpcAddress;
                }
            }

            var service = K8SRequestHelper.GetClient().ReadNamespacedService(GlobalSetting.LauncherServiceName, chainId);
            var address = "http://" + service.Status.LoadBalancer.Ingress.FirstOrDefault().Hostname + ":" + GlobalSetting.RpcPort;

            lock (_lock)
            {
                if (serviceUrl != null)
                {
                    ServiceUrlConfig.Instance.ServiceUrls[chainId].RpcAddress = address;
                }
                else
                {
                    ServiceUrlConfig.Instance.ServiceUrls.Add(chainId, new ServiceUrl {RpcAddress = address});
                }

                SaveConfigToFile();
            }
            
            return address;
        }

        public static string GetMonitorRpcAddress(string chainId)
        {
            if (ServiceUrlConfig.Instance.ServiceUrls.TryGetValue(chainId, out var serviceUrl))
            {
                if (!string.IsNullOrWhiteSpace(serviceUrl.MonitorRpcAddress))
                {
                    return serviceUrl.MonitorRpcAddress;
                }
            }

            var service = K8SRequestHelper.GetClient().ReadNamespacedService(GlobalSetting.MonitorServiceName, chainId);
            var address = "http://" + service.Status.LoadBalancer.Ingress.FirstOrDefault().Hostname + ":" + GlobalSetting.MonitorPort;

            lock (_lock)
            {
                if (serviceUrl != null)
                {
                    ServiceUrlConfig.Instance.ServiceUrls[chainId].MonitorRpcAddress = address;
                }
                else
                {
                    ServiceUrlConfig.Instance.ServiceUrls.Add(chainId, new ServiceUrl {MonitorRpcAddress = address});
                }

                SaveConfigToFile();
            }
            
            return address;
        }

        private static void SaveConfigToFile()
        {
            var configJson =  JsonSerializer.Instance.Serialize(ServiceUrlConfig.Instance);
            File.WriteAllText(Path.Combine(ApplicationHelpers.GetDefaultDataDir(), "config", "serviceurl.json"), configJson);
        }
    }
}