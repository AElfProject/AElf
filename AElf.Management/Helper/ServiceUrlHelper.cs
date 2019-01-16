using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AElf.Common.Application;
using AElf.Configuration;
using k8s;

namespace AElf.Management.Helper
{
    public class ServiceUrlHelper0
    {
        private static object _lock = new object();

        public static string GetRpcAddress(string chainId, Dictionary<string, ServiceUrl> serviceUrls)
        {
            if (serviceUrls.TryGetValue(chainId, out var serviceUrl))
            {
                if (!string.IsNullOrWhiteSpace(serviceUrl.RpcAddress))
                {
                    return serviceUrl.RpcAddress;
                }
            }

//            var service = K8SRequestHelper.GetClient().ReadNamespacedService(GlobalSetting.LauncherServiceName, chainId);
//            var address = "http://" + service.Status.LoadBalancer.Ingress.FirstOrDefault()?.Hostname + ":" + GlobalSetting.RpcPort;
//
//            lock (_lock)
//            {
//                if (serviceUrl != null)
//                {
//                    serviceUrls[chainId].RpcAddress = address;
//                }
//                else
//                {
//                    serviceUrls.Add(chainId, new ServiceUrl {RpcAddress = address});
//                }
//
//                SaveConfigToFile();
//            }

//            return address;
            throw new Exception();
        }

        public static string GetMonitorRpcAddress(string chainId, Dictionary<string, ServiceUrl> serviceUrls)
        {
            if (serviceUrls.TryGetValue(chainId, out var serviceUrl))
            {
                if (!string.IsNullOrWhiteSpace(serviceUrl.MonitorRpcAddress))
                {
                    return serviceUrl.MonitorRpcAddress;
                }
            }

//            var service = K8SRequestHelper.GetClient().ReadNamespacedService(GlobalSetting.MonitorServiceName, chainId);
//            var address = "http://" + service.Status.LoadBalancer.Ingress.FirstOrDefault().Hostname + ":" + GlobalSetting.MonitorPort;
//
//            lock (_lock)
//            {
//                if (serviceUrl != null)
//                {
//                    serviceUrls[chainId].MonitorRpcAddress = address;
//                }
//                else
//                {
//                    serviceUrls.Add(chainId, new ServiceUrl {MonitorRpcAddress = address});
//                }
//
//                SaveConfigToFile();
//            }
//
//            return address;
            throw new Exception();
        }

        private static void SaveConfigToFile()
        {
            throw new Exception();
        }
    }
}