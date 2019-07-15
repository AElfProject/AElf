using System;
using System.Collections.Generic;
using System.Linq;
using Anemonis.AspNetCore.JsonRpc;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AElf.OS.Rpc
{
    internal static class RpcServerHelper
    {
        private static List<Type> GetServiceTypes(IServiceCollection scope)
        {
            return scope.Where(p =>
                    p.ImplementationType != null && typeof(IJsonRpcService).IsAssignableFrom(p.ServiceType))
                .Select(p => p.ImplementationType).Distinct().ToList();
        }

        private static object Resolve(IServiceProvider scope, Type type)
        {
            return scope.GetService(type);
        }

        private static void AddJsonRpcService(IServiceCollection services, Type type)
        {
            var methodInfo = typeof(JsonRpcServicesExtensions).GetMethod("AddJsonRpcService");
            if (methodInfo == null)
            {
                throw new InvalidOperationException(
                    "Cannot find extension method AddJsonRpcService for IServiceCollection.");
            }

            var methodInfoGeneric = methodInfo.MakeGenericMethod(new[] {type});
            methodInfoGeneric.Invoke(services, new object[] {services, null});
        }

        private static void UseJsonRpcService(IApplicationBuilder appBuilder, Type type,
            PathString path = default(PathString))
        {
            var methodInfo = typeof(JsonRpcBuilderExtensions).GetMethod("UseJsonRpcService");
            if (methodInfo == null)
            {
                throw new InvalidOperationException(
                    "Cannot find extension method UseJsonRpcService for IApplicationBuilder.");
            }

            var methodInfoGeneric = methodInfo.MakeGenericMethod(new[] {type});
            methodInfoGeneric.Invoke(appBuilder, new object[] {appBuilder, path});
        }

        internal static void ConfigureServices(IServiceCollection services)
        {
            var types = GetServiceTypes(services);

            foreach (var serviceType in types)
            {
                AddJsonRpcService(services, serviceType);
            }
        }

        internal static void Configure(IApplicationBuilder appBuilder, IServiceCollection scope)
        {
            var types = GetServiceTypes(scope);
            foreach (var serviceType in types)
            {
                var attributes = serviceType.GetCustomAttributes(typeof(PathAttribute), false);
                if (attributes.Length == 0)
                {
                    throw new Exception($"Json rpc service {serviceType} doesn't have a Path attribute.");
                }

                UseJsonRpcService(appBuilder, serviceType, ((PathAttribute) attributes[0]).Path);
            }
        }
    }
}