using System;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using Autofac.Core;
using Community.AspNetCore;
using Community.AspNetCore.JsonRpc;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AElf.RPC
{
    internal static class RpcServerHelpers
    {
        private static List<Type> GetServiceTypes(ILifetimeScope scope)
        {
            var types = scope.ComponentRegistry.Registrations
                .SelectMany(r => r.Services.OfType<IServiceWithType>(), (r, s) => new {r, s})
                .Where(rs => typeof(IJsonRpcService).IsAssignableFrom(rs.s.ServiceType))
                .Select(rs => rs.r.Activator.LimitType).ToList();
            Console.WriteLine(string.Join(" type ", types));
            return types;
        }

        private static object Resolve(ILifetimeScope scope, Type type)
        {
            var methodInfo = typeof(ResolutionExtensions).GetMethod("Resolve", new []
            {
                typeof(IComponentContext)
            });
            if (methodInfo == null)
            {
                throw new InvalidOperationException(
                    "Cannot find extension method Resolve(IComponentContext) for ResolutionExtensions.");
            }
            var methodInfoGeneric = methodInfo.MakeGenericMethod(new[] { type });
            return methodInfoGeneric.Invoke(scope, new object[] { scope });
        }
        
        private static void AddJsonRpcService(IServiceCollection services, Type type)
        {
            var methodInfo = typeof(JsonRpcServicesExtensions).GetMethod("AddJsonRpcService");
            if (methodInfo == null)
            {
                throw new InvalidOperationException(
                    "Cannot find extension method AddJsonRpcService for IServiceCollection.");
            }
            var methodInfoGeneric = methodInfo.MakeGenericMethod(new[] { type });
            methodInfoGeneric.Invoke(services, new object[] { services , null});
            Console.WriteLine("adding " + type);
        }
        
        private static void UseJsonRpcService(IApplicationBuilder appBuilder, Type type, PathString path = default(PathString))
        {
            var methodInfo = typeof(JsonRpcBuilderExtensions).GetMethod("UseJsonRpcService");
            if (methodInfo == null)
            {
                throw new InvalidOperationException(
                    "Cannot find extension method UseJsonRpcService for IApplicationBuilder.");
            }
            var methodInfoGeneric = methodInfo.MakeGenericMethod(new[] { type });
            methodInfoGeneric.Invoke(appBuilder, new object[] { appBuilder , path});
        }
        
        internal static void ConfigureServices(IServiceCollection services, ILifetimeScope scope)
        {
            var types = GetServiceTypes(scope);
            foreach (var serviceType in types)
            {
                services.AddSingleton(serviceType, Resolve(scope, serviceType));
                AddJsonRpcService(services, serviceType);
            }
        }

        internal static void Configure(IApplicationBuilder appBuilder, ILifetimeScope scope)
        {
            var types = GetServiceTypes(scope);
            foreach (var serviceType in types)
            {
                var attributes = serviceType.GetCustomAttributes(typeof(PathAttribute),false);
                if (attributes.Length == 0)
                {
                    throw new Exception($"Json rpc service {serviceType} doesn't have a Path attribute.");
                }
                UseJsonRpcService(appBuilder, serviceType, ((PathAttribute)attributes[0]).Path);
            }   
        }
    }
}