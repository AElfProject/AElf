using System;
using System.IO;
using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Exporters.Xml;
using BenchmarkDotNet.Reports;
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
            var config = new DebugInProcessConfig()
                .WithSummaryStyle(SummaryStyle.Default.WithMaxParameterColumnWidth(50))
                .AddExporter(XmlExporter.Default)
                .AddExporter(HtmlExporter.Default)
                .AddExporter(new HtmlSummaryExporter())
                .AddExporter(CsvExporter.Default); 
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);        }
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