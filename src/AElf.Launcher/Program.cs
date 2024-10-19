using System;
using System.IO;
using System.Reflection;
using AElf.ExceptionHandler;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Launcher;

internal partial class Program
{
    private static void RegisterAssemblyResolveEvent()
    {
        var currentDomain = AppDomain.CurrentDomain;

        currentDomain.AssemblyResolve += OnAssemblyResolve;
    }

    private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
    {
        var folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
        if (!File.Exists(assemblyPath))
        {
            if (assemblyPath.Contains("Contract"))
            {
                assemblyPath = assemblyPath.Substring(0,
                    assemblyPath.IndexOf("bin", StringComparison.Ordinal) - 1);
                assemblyPath = Path.Combine(assemblyPath, "contracts", new AssemblyName(args.Name).Name + ".dll");
            }
            else
            {
                return null;
            }
        }

        var assembly = Assembly.LoadFrom(assemblyPath);
        return assembly;
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(Program),
        MethodName = nameof(HandleExceptionWhileStartingNode))]
    public static void Main(string[] args)
    {
        RegisterAssemblyResolveEvent();
        CreateHostBuilder(args).Build().Run();
    }

    // create default https://github.com/aspnet/MetaPackages/blob/master/src/Microsoft.AspNetCore/WebHost.cs
    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureLogging(builder => { builder.ClearProviders(); })
            .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
            .UseAutofac();
    }
}