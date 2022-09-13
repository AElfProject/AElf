using System;
using System.IO;
using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Volo.Abp;

namespace AElf.Benchmark;

internal class Program
{
    private static void Main(string[] args)
    {
        RegisterAssemblyResolveEvent();
        using (var application = AbpApplicationFactory.Create<BenchmarkAElfModule>(options =>
               {
                   options.UseAutofac();
               }))
        {
            application.Initialize();
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new DebugInProcessConfig());
        }
    }

    private static void RegisterAssemblyResolveEvent()
    {
        var currentDomain = AppDomain.CurrentDomain;

        currentDomain.AssemblyResolve += OnAssemblyResolve;
    }

    private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
    {
        var folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
        if (!File.Exists(assemblyPath)) return null;
        var assembly = Assembly.LoadFrom(assemblyPath);
        return assembly;
    }
}