using System;
using System.IO;
using System.Reflection;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using Volo.Abp;

namespace AElf.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            AddSameFolderLoader();
            using (var application = AbpApplicationFactory.Create<BenchmarkAElfModule>(options =>
            {
                options.UseAutofac();
            }))
            {
                application.Initialize();
                BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new DebugInProcessConfig());          
            }
        }
        
        static void AddSameFolderLoader()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;

            currentDomain.AssemblyResolve += LoadFromSameFolder;
        }

        static Assembly LoadFromSameFolder(object sender, ResolveEventArgs args)
        {
            string folderPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
            if (!File.Exists(assemblyPath)) return null;
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            return assembly;
        }
    }
}