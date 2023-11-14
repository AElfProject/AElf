// using Microsoft.Extensions.Configuration;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Hosting;
// using AElf.Kernel.SmartContract.Silo;
// using AElf.Kernel.SmartContract.Silo.Extensions;
// using AElf.OS;
// using AElf.Silo.Launcher;
// using AElf.WebApp.Application.Chain;
// using Microsoft.AspNetCore.Hosting;
// using Serilog;
// using Serilog.Events;
//
// namespace AElf.Kernel.SmartContract.Silo;
//
// public class Program
// {
//     public async static Task<int> Main(string[] args)
//     {
//         var configuration = new ConfigurationBuilder()
//             .AddJsonFile("appsettings.json")
//             //.AddJsonFile("appsetting.ParallelExecution.json")
//             .Build();
//         Log.Logger = new LoggerConfiguration()
// #if DEBUG
//             .MinimumLevel.Debug()
// #else
//             .MinimumLevel.Information()
// #endif
//             .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
//             .Enrich.FromLogContext()
//             .ReadFrom.Configuration(configuration)
//             // .WriteTo.Async(c => c.File("Logs/logs.txt"))
// #if DEBUG
//             .WriteTo.Async(c => c.Console())
// #endif
//             .CreateLogger();
//         try
//         {
//             Log.Information("Starting AElf.Silo.Launcher.");
//
//             await CreateHostBuilder(args).Build().RunAsync();
//
//             return 0;
//         }
//         catch (Exception ex)
//         {
//             Log.Fatal(ex, "Host terminated unexpectedly!");
//             return 1;
//         }
//         finally
//         {
//             Log.CloseAndFlush();
//         }
//     }
//     
//    
//     internal static IHostBuilder CreateHostBuilder(string[] args) =>
//         Host.CreateDefaultBuilder(args)
//             .ConfigureServices((hostcontext, services) =>
//             {
//                 services.AddApplication<AElfSiloLauncherModule>();
//             })
//             .UseOrleansSnapshot()
//             .UseAutofac()
//             .UseSerilog();
// }
using System;
using System.IO;
using System.Reflection;
using AElf.Kernel.SmartContract.Silo.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AElf.Launcher;

internal class Program
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

    public static void Main(string[] args)
    {
        RegisterAssemblyResolveEvent();
        ILogger<Program> logger = NullLogger<Program>.Instance;
        try
        {
            CreateHostBuilder(args).Build().Run();
        }
        catch (Exception e)
        {
            if (logger == NullLogger<Program>.Instance)
                Console.WriteLine(e);
            logger.LogCritical(e, "program crashed");
        }
    }

    // create default https://github.com/aspnet/MetaPackages/blob/master/src/Microsoft.AspNetCore/WebHost.cs
    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureLogging(builder => { builder.ClearProviders(); })
            .ConfigureWebHostDefaults(webBuilder => { webBuilder.UseStartup<Startup>(); })
            .UseOrleansSnapshot()
            .UseAutofac();
    }
}